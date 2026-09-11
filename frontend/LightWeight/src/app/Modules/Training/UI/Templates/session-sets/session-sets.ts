import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { Set } from '../../../data/training-api.service';

export interface ExerciseSetGroup {
  exerciseId: string;
  sets: Set[];
}

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

  Sets = this.store.sets;

  groupedSets = computed<ExerciseSetGroup[]>(() => {
    const sortedSets = [...this.Sets()].sort((a, b) => a.exerciseId.localeCompare(b.exerciseId));
    const groups: ExerciseSetGroup[] = [];
    for (const set of sortedSets) {
      const lastGroup = groups[groups.length - 1];
      if (lastGroup && lastGroup.exerciseId === set.exerciseId) {
        lastGroup.sets.push(set);
      } else {
        groups.push({ exerciseId: set.exerciseId, sets: [set] });
      }
    }
    return groups;
  });

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

  selectSet(set: Set){
    this.router.navigate(['/training/exercisesettings', this.TemplateId, this.SessionId], {
      queryParams: { setId: set.id },
    });
  }

  async deleteSet(setId: string){
    await this.store.DeleteSetTemplate(setId);
  }

  async deleteSession(){
    if (!this.SessionId) return;
    const deleted = await this.store.DeleteTemplateSession(this.SessionId);
    if (deleted) {
      this.backToSessions();
    }
  }

  backToSessions(){
    this.router.navigate(['/training/sessiontemplate', this.TemplateId]);
  }
}
