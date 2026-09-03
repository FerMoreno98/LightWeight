import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { Set } from '../../../data/training-api.service';

@Component({
  selector: 'app-session-sets',
  standalone: true,
  imports: [],
  templateUrl: './session-sets.html',
  styleUrl: './session-sets.css',
})
export class SessionSets {
  private store = inject(TrainingStore);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  private _isLoadingSets = signal(true);
  isLoading = this._isLoadingSets.asReadonly();
  error = this.store.error;

  Sets: Set[] = [];

  TemplateId : string | null = '';
  SessionId : string | null = '';

  async ngOnInit(){
    this.TemplateId = this.route.snapshot.paramMap.get('templateid');
    this.SessionId = this.route.snapshot.paramMap.get('sessionid');
    await Promise.all([
      this.loadSets(),
      this.store.GetAllExercises()
    ]);
  }

  async loadSets(){
    if (!this.TemplateId || !this.SessionId) return;
    this._isLoadingSets.set(true);
    try {
      await this.store.GetSetsFromASessionTemplate(this.TemplateId, this.SessionId);
      this.Sets = this.store.sets();
    } finally {
      this._isLoadingSets.set(false);
    }
  }

  exerciseName(exerciseId: string): string {
    return this.store.exercises().find(e => e.id === exerciseId)?.name ?? 'Ejercicio';
  }

  addSet(){
    this.router.navigate(['/training/exercisesettings', this.TemplateId, this.SessionId]);
  }

  backToSessions(){
    this.router.navigate(['/training/sessiontemplate', this.TemplateId]);
  }
}
