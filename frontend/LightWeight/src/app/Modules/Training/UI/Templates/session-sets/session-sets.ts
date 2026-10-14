import { Component, computed, inject, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { Set } from '../../../data/training-api.service';
import { TECHNIQUE_LABELS } from '../../shared/cycle-format';
import { ExerciseRun, SuperSetUnit, buildUnits, groupConsecutive } from '../../shared/superset';

/** Consecutive planned sets of one exercise, with a stable key to select it */
export interface ExerciseSetGroup extends ExerciseRun<Set> {
  key: number;
  exerciseName: string;
}

@Component({
  selector: 'app-session-sets',
  standalone: true,
  imports: [NgTemplateOutlet],
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

  /** Exercises in the planned order (the same exercise planned twice in different places stays twice) */
  groupedSets = computed<ExerciseSetGroup[]>(() =>
    groupConsecutive(this.Sets()).map((run, key) => ({
      ...run,
      key,
      exerciseName: run.sets[0].exerciseName || 'Ejercicio eliminado',
    })));

  /** Exercises on their own and supersets (A, B...), each superset where its first exercise is */
  units = computed<SuperSetUnit<ExerciseSetGroup>[]>(() => buildUnits(this.groupedSets(), g => g.superSetGroupId));

  /** Selection of exercises to join in a superset */
  isSelecting = signal(false);
  selectedKeys = signal<number[]>([]);
  isSavingSuperSet = signal(false);
  /** A superset needs at least two different exercises */
  canCreateSuperSet = computed(() => {
    const selected = this.groupedSets().filter(g => this.selectedKeys().includes(g.key));
    return new globalThis.Set(selected.map(g => g.exerciseId)).size >= 2;
  });

  TemplateId : string | null = '';
  SessionId : string | null = '';

  async ngOnInit(){
    this.TemplateId = this.route.snapshot.paramMap.get('templateid');
    this.SessionId = this.route.snapshot.paramMap.get('sessionid');
    await this.loadSets();
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

  techniqueLabel(set: Set): string | null {
    return set.technique ? TECHNIQUE_LABELS[set.technique] : null;
  }

  addSet(){
    this.router.navigate(['/training/exercisesettings', this.TemplateId, this.SessionId]);
  }

  selectSet(set: Set){
    if (this.isSelecting()) return;
    this.router.navigate(['/training/exercisesettings', this.TemplateId, this.SessionId], {
      queryParams: { setId: set.id },
    });
  }

  async deleteSet(setId: string){
    await this.store.DeleteSetTemplate(setId);
    // Deleting a set may dissolve its superset in the backend, so the sets are reloaded
    await this.store.GetSetsFromASessionTemplate(this.TemplateId!, this.SessionId!);
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

  // ---------------------------------------------------------------- Supersets

  startSelecting(){
    this.selectedKeys.set([]);
    this.isSelecting.set(true);
  }

  cancelSelecting(){
    this.isSelecting.set(false);
    this.selectedKeys.set([]);
  }

  isSelected(group: ExerciseSetGroup): boolean {
    return this.selectedKeys().includes(group.key);
  }

  toggleSelected(group: ExerciseSetGroup){
    this.selectedKeys.update(keys => keys.includes(group.key) ? keys.filter(k => k !== group.key) : [...keys, group.key]);
  }

  async createSuperSet(){
    if (!this.canCreateSuperSet() || !this.TemplateId || !this.SessionId) return;
    const setIds = this.groupedSets()
      .filter(g => this.selectedKeys().includes(g.key))
      .flatMap(g => g.sets.map(s => s.id));
    this.isSavingSuperSet.set(true);
    try {
      if (await this.store.CreateTemplateSuperSet(this.TemplateId, this.SessionId, setIds)) this.cancelSelecting();
    } finally {
      this.isSavingSuperSet.set(false);
    }
  }

  async separate(groupId: string){
    if (!this.TemplateId || !this.SessionId) return;
    this.isSavingSuperSet.set(true);
    try {
      await this.store.RemoveTemplateSuperSet(this.TemplateId, this.SessionId, groupId);
    } finally {
      this.isSavingSuperSet.set(false);
    }
  }
}
