import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TrainingStore } from '../../../state/training.store';
import { ActivatedRoute, Router } from '@angular/router';
import { Exercise } from '../../../data/training-api.service';

const MUSCLE_GROUP_LABELS = ['Hombro', 'Espalda', 'Pecho', 'Bíceps', 'Tríceps', 'Glúteos', 'Cuádriceps', 'Isquios', 'Gemelos'];
const MUSCLE_GROUP_NAMES = ['Shoulder', 'Back', 'Chest', 'Biceps', 'Triceps', 'Glutes', 'Quads', 'Hamstring', 'Calves'];

@Component({
  selector: 'app-exercise-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './exercise-settings.html',
  styleUrl: './exercise-settings.css',
})
export class ExerciseSettings {
  private store = inject(TrainingStore);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

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
  expectedRIR: number | null = null;
  series: number | null = 1;
  emphasizedMuscleGroups: number[] = [];

  isEditMode = false;
  editingSetId: string | null = null;
  editingSuperSetGroupId: string | null = null;

  async ngOnInit(){
    this.TemplateId = this.route.snapshot.paramMap.get('templateid');
    this.SessionId = this.route.snapshot.paramMap.get('sessionid');
    const setId = this.route.snapshot.queryParamMap.get('setId');

    await this.store.GetAllExercises();
    this.Exercises = this.store.exercises();

    if (setId && this.TemplateId && this.SessionId){
      await this.store.GetSetsFromASessionTemplate(this.TemplateId, this.SessionId);
      const set = this.store.sets().find(s => s.id === setId);
      if (set){
        this.isEditMode = true;
        this.editingSetId = set.id;
        this.editingSuperSetGroupId = set.superSetGroupId;
        this.selectedExercise = this.Exercises.find(e => e.id === set.exerciseId) ?? null;
        this.min = set.repetitionRangeMin;
        this.max = set.repetitionRangeMax;
        this.expectedRIR = set.expectedRIR;
        this.isDropset = set.advanceTrainingTechniques === 'DropSet';
        this.isCluster = set.advanceTrainingTechniques === 'Cluster';
        this.isMyoRep = set.advanceTrainingTechniques === 'MyoRep';
        this.emphasizedMuscleGroups = set.aimMuscleGroups
          .map(muscle => MUSCLE_GROUP_NAMES.indexOf(muscle))
          .filter(index => index !== -1);
      }
    }
  }

  muscleGroupLabel(group: number): string {
    return MUSCLE_GROUP_LABELS[group] ?? 'Otro';
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
      this.router.navigate(['/training/sessionsets', this.TemplateId, this.SessionId]);
      return;
    }
    this.selectedExercise = null;
    this.min = null;
    this.max = null;
    this.isDropset = false;
    this.isCluster = false;
    this.isMyoRep = false;
    this.expectedRIR = null;
    this.series = 1;
    this.emphasizedMuscleGroups = [];
  }

  async onSubmit(){
    if (!this.selectedExercise) return;

    const emphasizedGroups = this.emphasizedMuscleGroups.length
      ? this.emphasizedMuscleGroups
      : this.selectedExercise.aimMuscleGroups;
    const aimMuscleGroups = emphasizedGroups.map(group => MUSCLE_GROUP_NAMES[group]);

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
          this.expectedRIR!,
          this.editingSuperSetGroupId
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
          this.expectedRIR!,
          this.series!,
          null
        );

    if (success){
      this.router.navigate(['/training/sessionsets', this.TemplateId, this.SessionId]);
    }
  }
}
