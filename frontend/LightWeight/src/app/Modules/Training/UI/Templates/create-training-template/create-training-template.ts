import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TrainingStore } from '../../../state/training.store';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-create-training-template',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './create-training-template.html',
  styleUrl: './create-training-template.css',
})
export class CreateTrainingTemplate {
  private store = inject(TrainingStore);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  isLoading = this.store.isLoading;
  error = this.store.error;
  programs = this.store.programs;

  programId = '';
  volumeLandmark = '';
  trainingDistribution = '';
  durationInDays: number | null = 7;
  order: number | null = 1;

  async ngOnInit(){
    await this.store.GetUserPrograms();
    const programId = this.route.snapshot.queryParamMap.get('programId');
    if (programId && this.programs().some(p => p.id === programId)){
      this.programId = programId;
    } else if (this.programs().length === 1){
      this.programId = this.programs()[0].id;
    }
    this.onProgramChange();
  }

  /** Suggests the next position inside the selected program */
  onProgramChange(){
    const program = this.programs().find(p => p.id === this.programId);
    this.order = program ? program.trainingTemplatesCount + 1 : 1;
  }

  async onSubmit(){
    const id = await this.store.CreateTrainingTemplate(
      this.programId,
      this.volumeLandmark,
      this.trainingDistribution,
      this.durationInDays!,
      this.order!
    )

    if (id){
      this.router.navigate(['/training/sessiontemplate', id]);
    }
  }
}
