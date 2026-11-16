import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TrainingStore } from '../../../state/training.store';
import { ActivatedRoute, Router, RouterLink, UrlTree } from '@angular/router';
import { Exercise } from '../../../data/training-api.service';
import { LATERALITY_LABELS, MUSCLE_GROUPS, muscleGroupLabel } from '../../shared/muscle-groups';
import { TrainingFlow } from '../../shared/training-flow';

@Component({
  selector: 'app-exercise-settings',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './exercise-settings.html',
  styleUrl: './exercise-settings.css',
})
export class ExerciseSettings {
  private store = inject(TrainingStore);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  protected flow = inject(TrainingFlow);

  isLoading = this.store.isLoading;
  error = this.store.error;

  TemplateId : string | null = '';
  SessionId : string | null = '';
  Exercises : Exercise[] = [];
  selectedExercise: Exercise | null = null;

  min: number | null = null;
  max: number | null = null;
  isDropset = false;
  isCluster = false;
  isMyoRep = false;
  expectedRPE: number | null = null;
  series: number | null = 1;
  emphasizedMuscleGroups: number[] = [];

  /** Back to the sets of the session. Built once from the route ids (null until they are read) */
  setsLink: UrlTree | null = null;

  isEditMode = false;
  editingSetId: string | null = null;

  async ngOnInit(){
    this.TemplateId = this.route.snapshot.paramMap.get('templateid');
    this.SessionId = this.route.snapshot.paramMap.get('sessionid');
    const setId = this.route.snapshot.queryParamMap.get('setId');
    if (this.TemplateId && this.SessionId) this.setsLink = this.flow.sessionSets(this.TemplateId, this.SessionId);

    await this.store.GetAllExercises();
    this.Exercises = this.store.exercises();

    if (setId && this.TemplateId && this.SessionId){
      await this.store.GetSetsFromASessionTemplate(this.TemplateId, this.SessionId);
      const set = this.store.sets().find(s => s.id === setId);
      if (set){
        this.isEditMode = true;
        this.editingSetId = set.id;
        this.selectedExercise = this.Exercises.find(e => e.id === set.exerciseId) ?? null;
        this.min = set.repetitionRangeMin;
        this.max = set.repetitionRangeMax;
        this.expectedRPE = set.expectedRPE;
        this.isDropset = set.technique === 'DropSet';
        this.isCluster = set.technique === 'Cluster';
        this.isMyoRep = set.technique === 'MyoRep';
        this.emphasizedMuscleGroups = set.aimMuscleGroups
          .map(muscle => (MUSCLE_GROUPS as string[]).indexOf(muscle))
          .filter(index => index !== -1);
      }
    }
  }

  readonly lateralityLabels = LATERALITY_LABELS;

  /** Numeric values of every muscle group, to filter by them */
  readonly muscleGroups = MUSCLE_GROUPS.map((_, index) => index);
  /** Text typed in the search box */
  searchTerm = signal('');
  /** Primary muscle group to filter by (null: all of them) */
  groupFilter = signal<number | null>(null);

  /** Exercises whose name contains every word searched (ignoring case and accents) and that mainly target the group */
  filteredExercises = computed(() => {
    const words = normalize(this.searchTerm()).split(/\s+/).filter(Boolean);
    const group = this.groupFilter();
    return this.store.exercises().filter(exercise =>
      (group === null || exercise.primaryMuscleGroups.includes(group))
      && words.every(word => normalize(exercise.name).includes(word)));
  });

  /** Selects a group, or goes back to all of them when it was already selected */
  toggleGroupFilter(group: number | null){
    this.groupFilter.set(this.groupFilter() === group ? null : group);
  }

  clearFilters(){
    this.searchTerm.set('');
    this.groupFilter.set(null);
  }

  isPrimaryMuscleGroup(exercise: Exercise, group: number): boolean {
    return exercise.primaryMuscleGroups.includes(group);
  }

  muscleGroupLabel(group: number): string {
    return muscleGroupLabel(group);
  }

  selectExercise(exercise: Exercise){
    this.selectedExercise = exercise;
  }

  isMuscleGroupEmphasized(group: number): boolean {
    return this.emphasizedMuscleGroups.includes(group);
  }

  toggleEmphasizedMuscleGroup(group: number){
    this.emphasizedMuscleGroups = this.isMuscleGroupEmphasized(group)
      ? this.emphasizedMuscleGroups.filter(g => g !== group)
      : [...this.emphasizedMuscleGroups, group];
  }

  backToExercises(){
    if (this.isEditMode){
      this.backToSets();
      return;
    }
    this.selectedExercise = null;
    this.min = null;
    this.max = null;
    this.isDropset = false;
    this.isCluster = false;
    this.isMyoRep = false;
    this.expectedRPE = null;
    this.series = 1;
    this.emphasizedMuscleGroups = [];
  }

  async onSubmit(){
    if (!this.selectedExercise) return;

    const emphasizedGroups = this.emphasizedMuscleGroups.length
      ? this.emphasizedMuscleGroups
      : this.selectedExercise.aimMuscleGroups;
    const aimMuscleGroups = emphasizedGroups.map(group => MUSCLE_GROUPS[group]);

    const success = this.isEditMode
      ? await this.store.UpdateTemplateSet(
          this.SessionId,
          this.editingSetId,
          this.min!,
          this.max!,
          this.isDropset,
          this.isCluster,
          this.isMyoRep,
          aimMuscleGroups,
          this.expectedRPE!
        )
      : await this.store.CreateTemplateSet(
          this.selectedExercise.id,
          this.SessionId,
          this.min!,
          this.max!,
          this.isDropset,
          this.isCluster,
          this.isMyoRep,
          aimMuscleGroups,
          this.expectedRPE!,
          this.series!
        );

    if (success){
      this.backToSets();
    }
  }

  private backToSets(){
    if (this.setsLink) this.router.navigateByUrl(this.setsLink);
  }
}

/** Lowercase and without accents, so "jalon" finds "Jalón" */
function normalize(text: string): string {
  return text.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '');
}
