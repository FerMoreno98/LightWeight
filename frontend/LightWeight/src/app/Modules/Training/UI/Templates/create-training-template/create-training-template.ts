import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TrainingStore } from '../../../state/training.store';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-training-template',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './create-training-template.html',
  styleUrl: './create-training-template.css',
})
export class CreateTrainingTemplate {
    private store = inject(TrainingStore);
  private router = inject(Router);

  isLoading = this.store.isLoading;
  error = this.store.error;

  name = '';
  volumeLandmark = '';
  trainingDistribution = '';

  async onSubmit(values:{
    name : string,
    volumeLandmark: string,
    trainingDistribution: string
  }){
    const id = await this.store.CreateTrainingTemplate(
      values.name,
      values.volumeLandmark,
      values.trainingDistribution
    )

    if (id){
      this.router.navigate(['/training/sessiontemplate', id]);
    }
  }
}
