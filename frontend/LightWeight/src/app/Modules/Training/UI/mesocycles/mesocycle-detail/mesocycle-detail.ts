import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { MesocycleMicrocycle, MesocycleTemplate } from '../../../data/training-api.service';
import { TRAINING_DISTRIBUTION_LABELS, VOLUME_LANDMARK_LABELS, formatCyclePeriod } from '../../shared/cycle-format';
import { ConfirmDialog } from '../../../../../Shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-mesocycle-detail',
  standalone: true,
  imports: [RouterLink, ConfirmDialog],
  templateUrl: './mesocycle-detail.html',
  styleUrl: './mesocycle-detail.css',
})
export class MesocycleDetail {
  private store = inject(TrainingStore);
  private route = inject(ActivatedRoute);

  mesocycleId = '';
  mesocycle = this.store.mesocycleDetail;
  isLoading = this.store.isLoading;
  error = this.store.error;

  isFinished = computed(() => !!this.mesocycle()?.finishedAt);
  nextWeek = computed(() => (this.mesocycle()?.microcycles.at(-1)?.weekNumber ?? 0) + 1);
  /** The week in progress: a new one can only be added once it is finished */
  activeWeek = computed(() => this.mesocycle()?.microcycles.find(m => !m.finishedAt) ?? null);

  isAdding = false;
  isFinishing = signal(false);
  isConfirmingFinish = signal(false);
  isFinishingWeek = signal(false);
  isConfirmingFinishWeek = signal(false);
  /** Template being added as the next week, to disable the cards and avoid adding it twice */
  addingTemplateId = signal<string | null>(null);

  async ngOnInit() {
    this.mesocycleId = this.route.snapshot.paramMap.get('id')!;
    await this.store.GetMesocycleDetail(this.mesocycleId);
  }

  period(): string {
    const mesocycle = this.mesocycle();
    return mesocycle ? formatCyclePeriod(mesocycle.startedAt, mesocycle.finishedAt) : '';
  }

  weekPeriod(week: MesocycleMicrocycle): string {
    return formatCyclePeriod(week.startedAt, week.finishedAt);
  }

  volumeLandmarkLabel(template: MesocycleTemplate): string {
    return VOLUME_LANDMARK_LABELS[template.volumeLandmark] ?? template.volumeLandmark;
  }

  trainingDistributionLabel(template: MesocycleTemplate): string {
    return TRAINING_DISTRIBUTION_LABELS[template.trainingDistribution] ?? template.trainingDistribution;
  }

  showAddWeek() {
    this.isAdding = true;
  }

  backToWeeks() {
    this.isAdding = false;
  }

  async addWeek(template: MesocycleTemplate) {
    if (this.addingTemplateId()) return;
    this.addingTemplateId.set(template.id);
    try {
      const added = await this.store.CreateMicrocycle(this.mesocycleId, template.id);
      if (added) this.isAdding = false;
    } finally {
      this.addingTemplateId.set(null);
    }
  }

  askFinishMesocycle() {
    this.isConfirmingFinish.set(true);
  }

  cancelFinishMesocycle() {
    this.isConfirmingFinish.set(false);
  }

  async finishMesocycle() {
    this.isFinishing.set(true);
    try {
      await this.store.FinishMesocycle(this.mesocycleId);
    } finally {
      this.isFinishing.set(false);
      this.isConfirmingFinish.set(false);
    }
  }

  askFinishWeek() {
    this.isConfirmingFinishWeek.set(true);
  }

  cancelFinishWeek() {
    this.isConfirmingFinishWeek.set(false);
  }

  async finishWeek() {
    const week = this.activeWeek();
    if (!week) return;
    this.isFinishingWeek.set(true);
    try {
      if (await this.store.FinishMicrocycle(week.id)) {
        await this.store.GetMesocycleDetail(this.mesocycleId);
      }
    } finally {
      this.isFinishingWeek.set(false);
      this.isConfirmingFinishWeek.set(false);
    }
  }
}
