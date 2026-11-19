import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TrainingStore } from '../../../state/training.store';
import { ActivatedRoute, RouterLink, UrlTree } from '@angular/router';
import { SeriesPerGroupPerSession } from '../../../data/training-api.service';
import { TrainingFlow } from '../../shared/training-flow';
import { ConfirmDialog } from '../../../../../Shared/confirm-dialog/confirm-dialog';
import { SessionCard } from '../session-card/session-card';

@Component({
  selector: 'app-create-session-template',
  standalone: true,
  imports: [FormsModule, RouterLink, SessionCard, ConfirmDialog],
  templateUrl: './create-session-template.html',
  styleUrl: './create-session-template.css',
})
export class CreateSessionTemplate {
  private store = inject(TrainingStore);
  private route = inject(ActivatedRoute);
  protected flow = inject(TrainingFlow);
  /** Built once: a new UrlTree on every check would make [routerLink] change all the time */
  readonly templatesListLink = this.flow.templatesList();

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

  /**
   * Link of each session card, built once per list of sessions: a new UrlTree on every check
   * would make [routerLink] change all the time
   */
  sessionLinks = computed(() => {
    const links = new Map<string, UrlTree>();
    if (this.idTrainingTemplate)
      for (const session of this.Sessions())
        links.set(session.sessionId, this.flow.sessionSets(this.idTrainingTemplate, session.sessionId));
    return links;
  });

  /** Session waiting for the confirmation to delete it */
  deleteTarget = signal<SeriesPerGroupPerSession | null>(null);
  isDeleting = signal(false);

  askDelete(session: SeriesPerGroupPerSession){
    this.deleteTarget.set(session);
  }

  cancelDelete(){
    if (!this.isDeleting()) this.deleteTarget.set(null);
  }

  async confirmDelete(){
    const session = this.deleteTarget();
    if (!session) return;
    this.isDeleting.set(true);
    try {
      await this.store.DeleteTemplateSession(session.sessionId);
    } finally {
      this.isDeleting.set(false);
      this.deleteTarget.set(null);
    }
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
