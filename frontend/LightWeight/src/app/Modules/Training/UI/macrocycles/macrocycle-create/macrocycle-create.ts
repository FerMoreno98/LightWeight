import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';

@Component({
  selector: 'app-macrociclo-create',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './macrocycle-create.html',
  styleUrl: './macrocycle-create.css',
})
export class MacrocicloCreatePage {
  private store = inject(TrainingStore);
  private router = inject(Router);

  isLoading = this.store.isLoading;
  error = this.store.error;

  async onSubmit(values: {
    startAt: string;
    endAt?: string;
    stage: string;
    periodization: string;
    comments?: string;
  }) {
    const success = await this.store.CreateMacrocycle(
      new Date(values.startAt + 'T00:00:00'),
      values.endAt ? new Date(values.endAt + 'T00:00:00') : null,
      values.stage,
      values.periodization,
      values.comments ?? null,
    );
    if (success) {
      this.router.navigate(['/training/macrocycle']);
    }
  }

  cancel() {
    this.router.navigate(['/training/macrocycle']);
  }
}
