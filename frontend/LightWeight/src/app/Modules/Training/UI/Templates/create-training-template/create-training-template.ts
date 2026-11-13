import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TrainingStore } from '../../../state/training.store';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TrainingFlow } from '../../shared/training-flow';

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
  protected flow = inject(TrainingFlow);
  /** Built once: a new UrlTree on every check would make [routerLink] change all the time */
  readonly createProgramLink = this.flow.createProgram();

  isLoading = this.store.isLoading;
  error = this.store.error;
  programs = this.store.programs;

  programId = '';
  name = '';
  volumeLandmark = '';
  trainingDistribution = '';
  durationInDays: number | null = 7;

  async ngOnInit(){
    await this.store.GetUserPrograms();
    // In the wizard the program is part of the path; in the app it comes as ?programId=
    const programId = this.route.snapshot.paramMap.get('programId') ?? this.route.snapshot.queryParamMap.get('programId');
    if (programId && this.programs().some(p => p.id === programId)){
      this.programId = programId;
    } else if (this.programs().length === 1){
      this.programId = this.programs()[0].id;
    }
  }

  async onSubmit(){
    const id = await this.store.CreateTrainingTemplate(
      this.programId,
      this.name.trim(),
      this.volumeLandmark,
      this.trainingDistribution,
      this.durationInDays!
    )

    if (id){
      this.router.navigateByUrl(this.flow.templateSessions(id));
    }
  }
}
