import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TrainingStore } from '../../../state/training.store';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MuscleGroup, SeriesPerGroupPerSession } from '../../../data/training-api.service';

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

  Sessions: SeriesPerGroupPerSession[] = [];
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
    this.Sessions = this.store.seriesPerGroupPerSession();
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

  seriesEntries(session: SeriesPerGroupPerSession): [string, number][] {
    return Object.entries(session.series).map(([group, count]) => [MUSCLE_GROUP_LABELS[group as MuscleGroup] ?? group, count]);
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
