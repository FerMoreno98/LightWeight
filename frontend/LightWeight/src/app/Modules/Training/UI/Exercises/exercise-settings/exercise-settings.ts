import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TrainingStore } from '../../../state/training.store';
import { ActivatedRoute, Router } from '@angular/router';
import { Exercise } from '../../../data/training-api.service';

const MUSCLE_GROUP_LABELS = ['Hombro', 'Espalda', 'Pecho', 'Bíceps', 'Tríceps', 'Glúteos', 'Cuádriceps', 'Isquios', 'Gemelos'];
const MUSCLE_GROUP_NAMES = ['Shoulder', 'Back', 'Chest', 'Biceps', 'Triceps', 'Glutes', 'Quads', 'Hamstring', 'calves'];

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
  emphasizedMuscleGroups: number[] = [];

  async ngOnInit(){
    this.TemplateId = this.route.snapshot.paramMap.get('templateid');
    this.SessionId = this.route.snapshot.paramMap.get('sessionid');
    await this.store.GetAllExercises();
    this.Exercises = this.store.exercises();
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
    this.selectedExercise = null;
    this.min = null;
    this.max = null;
    this.isDropset = false;
    this.isCluster = false;
    this.isMyoRep = false;
    this.expectedRIR = null;
    this.emphasizedMuscleGroups = [];
  }

  async onSubmit(){
    if (!this.selectedExercise) return;

    const aimMuscleGroups = this.emphasizedMuscleGroups.map(group => MUSCLE_GROUP_NAMES[group]);

    const success = await this.store.CreateTemplateSet(
      this.selectedExercise.id,
      this.SessionId,
      this.min!,
      this.max!,
      this.isDropset,
      this.isCluster,
      this.isMyoRep,
      aimMuscleGroups,
      this.expectedRIR!,
      null
    );

    if (success){
      this.backToExercises();
      this.router.navigate(['/training/sessionsets', this.TemplateId, this.SessionId]);
    }
  }
}
