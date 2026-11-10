import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TrainingStore } from '../../../state/training.store';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SeriesPerGroupPerSession } from '../../../data/training-api.service';
import { muscleGroupLabel } from '../../shared/muscle-groups';

@Component({
  selector: 'app-create-session-template',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './create-session-template.html',
  styleUrl: './create-session-template.css',
})
export class CreateSessionTemplate {
  private store = inject(TrainingStore);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  isLoading = this.store.isLoading;
  error = this.store.error;

  Sessions = this.store.seriesPerGroupPerSession;
  isAdding = false;

  name = '';

  idTrainingTemplate : string | null = ''

  async ngOnInit(){
    this.idTrainingTemplate = this.route.snapshot.paramMap.get("id");
    await this.loadSessions();
  }

  async loadSessions(){
    if (!this.idTrainingTemplate) return;
    await this.store.GetSeriesPerMuscleGroupPerSession(this.idTrainingTemplate);
  }

  showAddForm(){
    this.isAdding = true;
  }

  backToSessions(){
    this.isAdding = false;
    this.name = '';
  }

  goToSession(session: SeriesPerGroupPerSession){
    this.router.navigate(['/training/sessionsets', this.idTrainingTemplate, session.sessionId]);
  }

  async deleteSession(session: SeriesPerGroupPerSession){
    await this.store.DeleteTemplateSession(session.sessionId);
  }

  seriesEntries(session: SeriesPerGroupPerSession): [string, number][] {
    return Object.entries(session.series).map(([group, count]) => [muscleGroupLabel(group), count]);
  }

  async onSubmit(values:{
    name : string

  }){
    const id = await this.store.CreateTemplateSession(
      this.idTrainingTemplate,
      values.name
    )
    if(id){
      await this.loadSessions();
      this.backToSessions();
    }
  }
}
