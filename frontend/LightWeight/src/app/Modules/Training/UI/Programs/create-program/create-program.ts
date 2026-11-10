import { Component, inject } from '@angular/core';
import { Location } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { MuscleGroup } from '../../../data/training-api.service';
import { MUSCLE_GROUPS, MUSCLE_GROUP_LABELS } from '../../shared/muscle-groups';

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
  private location = inject(Location);
  /** Whether the user arrived here from another page of the app (so Cancel can go back to it) */
  private hasPreviousPage = !!this.router.lastSuccessfulNavigation()?.previousNavigation;

  isLoading = this.store.isLoading;
  error = this.store.error;

  muscleGroups = MUSCLE_GROUPS;

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

  /** Returns to the page that opened the form (templates list as fallback on direct access) */
  cancel() {
    if (this.hasPreviousPage) {
      this.location.back();
    } else {
      this.router.navigate(['/training/trainingtemplates']);
    }
  }
}
