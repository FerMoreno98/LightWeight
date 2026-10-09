import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { MuscleGroup } from '../../../data/training-api.service';

const MUSCLE_GROUP_LABELS: Record<MuscleGroup, string> = {
  Shoulder: 'Hombro',
  Back: 'Espalda',
  Chest: 'Pecho',
  Biceps: 'Bíceps',
  Triceps: 'Tríceps',
  Glutes: 'Glúteos',
  Quads: 'Cuádriceps',
  Hamstring: 'Isquios',
  Calves: 'Gemelos',
};

@Component({
  selector: 'app-create-program',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './create-program.html',
  styleUrl: './create-program.css',
})
export class CreateProgram {
  private store = inject(TrainingStore);
  private router = inject(Router);

  isLoading = this.store.isLoading;
  error = this.store.error;

  muscleGroups = Object.keys(MUSCLE_GROUP_LABELS) as MuscleGroup[];

  name = '';
  periodization = '';
  aimMuscleGroups: MuscleGroup[] = [];

  muscleGroupLabel(group: MuscleGroup): string {
    return MUSCLE_GROUP_LABELS[group];
  }

  isMuscleGroupSelected(group: MuscleGroup): boolean {
    return this.aimMuscleGroups.includes(group);
  }

  toggleMuscleGroup(group: MuscleGroup) {
    this.aimMuscleGroups = this.isMuscleGroupSelected(group)
      ? this.aimMuscleGroups.filter(g => g !== group)
      : [...this.aimMuscleGroups, group];
  }

  async onSubmit() {
    const id = await this.store.CreateProgram(
      this.name,
      this.periodization,
      this.aimMuscleGroups
    );

    if (id) {
      this.router.navigate(['/training/trainingtemplate'], { queryParams: { programId: id } });
    }
  }

  cancel() {
    this.router.navigate(['/training/trainingtemplates']);
  }
}
