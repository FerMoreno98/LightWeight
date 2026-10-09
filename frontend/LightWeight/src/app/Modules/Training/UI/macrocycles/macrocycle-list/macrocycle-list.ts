import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { MacrocycleSummary } from '../../../data/training-api.service';
import { TRAINING_STAGE_LABELS, formatCyclePeriod } from '../../shared/cycle-format';

@Component({
  selector: 'app-macrocycle-list',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './macrocycle-list.html',
  styleUrl: './macrocycle-list.css',
})
export class MacrocycleList {
  private store = inject(TrainingStore);
  private router = inject(Router);

  error = this.store.error;
  // Local flag so the skeleton is only shown on the first load
  private _loaded = signal(false);
  loaded = this._loaded.asReadonly();

  /** There is at most one active macrocycle */
  activeMacrocycle = computed(() => this.store.macrocycles().find(m => !m.finishedAt) ?? null);
  finishedMacrocycles = computed(() => this.store.macrocycles().filter(m => m.finishedAt));

  async ngOnInit() {
    await this.store.GetUserMacrocycles();
    this._loaded.set(true);
  }

  stageLabel(macrocycle: MacrocycleSummary): string {
    return TRAINING_STAGE_LABELS[macrocycle.stage] ?? macrocycle.stage;
  }

  period(macrocycle: MacrocycleSummary): string {
    return formatCyclePeriod(macrocycle.startedAt, macrocycle.finishedAt);
  }

  open(macrocycle: MacrocycleSummary) {
    this.router.navigate(['/training/macrocycle', macrocycle.id]);
  }
}
