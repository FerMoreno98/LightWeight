import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { MesocycleTemplate, TrainingDistribution, VolumeLandmark } from '../../../data/training-api.service';
import { formatCyclePeriod } from '../../shared/cycle-format';
import { ConfirmDialog } from '../../../../../Shared/confirm-dialog/confirm-dialog';

const VOLUME_LANDMARK_LABELS: Record<VolumeLandmark, string> = {
  MV: 'MV — Maintenance Volume',
  MEV: 'MEV — Minimum Effective Volume',
  MAV: 'MAV — Maximum Adaptive Volume',
  MRV: 'MRV — Maximum Recoverable Volume',
};

const TRAINING_DISTRIBUTION_LABELS: Record<TrainingDistribution, string> = {
  PushPullLegs: 'Push Pull Legs',
  UpperLower: 'Upper Lower',
  Weider: 'Weider',
  Phat: 'PHAT',
  FullBody: 'Full Body',
  Other: 'Otra',
};

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

  isAdding = false;
  isFinishing = signal(false);
  isConfirmingFinish = signal(false);
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
}
