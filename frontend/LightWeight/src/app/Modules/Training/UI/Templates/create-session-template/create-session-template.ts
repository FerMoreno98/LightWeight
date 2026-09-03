import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TrainingStore } from '../../../state/training.store';
import { ActivatedRoute, Router } from '@angular/router';
import { Session } from '../../../data/training-api.service';

@Component({
  selector: 'app-create-session-template',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './create-session-template.html',
  styleUrl: './create-session-template.css',
})
export class CreateSessionTemplate {
  private store = inject(TrainingStore);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  isLoading = this.store.isLoading;
  error = this.store.error;

  Sessions: Session[] = [];
  isAdding = false;

  name = '';

  idTrainingTemplate : string | null = ''

  async ngOnInit(){
    this.idTrainingTemplate = this.route.snapshot.paramMap.get("id");
    await this.loadSessions();
  }

  async loadSessions(){
    if (!this.idTrainingTemplate) return;
    await this.store.GetSessionsFromATrainingTemplate(this.idTrainingTemplate);
    this.Sessions = this.store.sessions();
  }

  showAddForm(){
    this.isAdding = true;
  }

  backToSessions(){
    this.isAdding = false;
    this.name = '';
  }

  goToSession(session: Session){
    this.router.navigate(['/training/sessionsets', this.idTrainingTemplate, session.id]);
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
