import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TrainingStore } from '../../state/training.store';
import { MesocycleTemplate, MuscleGroup, Session, TrainingSetupStatus } from '../../data/training-api.service';
import { TRAINING_DISTRIBUTION_LABELS, formatCyclePeriod } from '../shared/cycle-format';
import { ConfirmDialog } from '../../../../Shared/confirm-dialog/confirm-dialog';
import { WeekSessionCard, WeekSessionStatus } from './week-session-card/week-session-card';

/**
 * Week the user is training now: the active week of the active mesocycle of the active macrocycle.
 * When some level is missing it shows the step to take (start a macrocycle, a mesocycle or plan the week)
 */
@Component({
  selector: 'app-current-week',
  standalone: true,
  imports: [RouterLink, ConfirmDialog, WeekSessionCard],
  templateUrl: './current-week.html',
  styleUrl: './current-week.css',
})
export class CurrentWeek {
  private store = inject(TrainingStore);

  current = this.store.currentMicrocycle;
  sessions = this.store.sessions;
  error = this.store.error;
  /** Templates of the mesocycle's program, to plan the next week */
  availableTemplates = computed(() => {
    const current = this.current();
    const mesocycle = this.store.mesocycleDetail();
    return current?.status === 'PendingPlanning' && mesocycle?.id === current.mesocycleId
      ? mesocycle.availableTemplates
      : [];
  });

  /** Setup status, only loaded without an active macrocycle: decides whether to offer the onboarding wizard */
  setup = signal<TrainingSetupStatus | null>(null);
  /** Someone who has never had a macrocycle gets the wizard instead of the bare "start a macrocycle" */
  offerWizard = computed(() => !!this.setup()?.isFirstTime);

  /** Own loading flag: the store one is shared by every request of the page */
  isLoading = signal(true);
  isFinishing = signal(false);
  isConfirmingFinish = signal(false);
  /** Template being planned as the next week, to disable the cards and avoid planning it twice */
  planningTemplateId = signal<string | null>(null);

  /** Planned sets per group of each session of the week's template, by session id (null: not loaded) */
  sessionsVolume = signal<Record<string, Partial<Record<MuscleGroup, number>>> | null>(null);
  /** What has been done of the session in progress, if there is one */
  inProgressProgress = signal<{ sessionId: string; sets: number; byGroup: Partial<Record<MuscleGroup, number>> } | null>(null);

  /** The sessions of the week with their state; the next one is the one in progress or, without one, the first pending */
  sessionCards = computed(() => {
    const started = this.current()?.trainingSessions ?? [];
    const cards = this.sessions().map((session, index) => {
      const run = started.find(s => s.templateSessionId === session.id);
      const status: WeekSessionStatus = !run ? 'pending' : run.finishedAt ? 'done' : 'inProgress';
      const progress = this.inProgressProgress();
      return {
        session,
        status,
        position: index + 1,
        finishedAt: run?.finishedAt ?? null,
        progress: status === 'inProgress' && progress?.sessionId === session.id ? progress : null,
        next: false,
      };
    });
    const next = cards.find(c => c.status === 'inProgress') ?? cards.find(c => c.status === 'pending');
    if (next) next.next = true;
    return cards;
  });

  /** Header of the week: sessions done and planned sets covered (whole sessions for the finished ones) */
  weekProgress = computed(() => {
    const cards = this.sessionCards();
    return {
      done: cards.filter(c => c.status === 'done').length,
      total: cards.length,
      setsDone: cards.reduce((sum, c) => sum + (c.status === 'done' ? c.session.setsCount : c.progress?.sets ?? 0), 0),
      setsTotal: cards.reduce((sum, c) => sum + c.session.setsCount, 0),
    };
  });

  async ngOnInit() {
    await this.load();
  }

  /** Loads the current week and what each state needs: the sessions of its template or the templates to plan it */
  async load() {
    this.isLoading.set(true);
    try {
      if (!(await this.store.GetCurrentMicrocycle())) return;
      const current = this.current();
      if (current?.status === 'InProgress' && current.microcycle?.templateName) {
        const templateId = current.microcycle.trainingTemplateId;
        const inProgress = current.trainingSessions.find(s => !s.finishedAt && s.templateSessionId);
        const [, volume, progress] = await Promise.all([
          this.store.GetSessionsFromATrainingTemplate(templateId),
          this.store.GetSessionsVolume(templateId),
          inProgress ? this.store.GetSessionProgress(templateId, inProgress.templateSessionId!) : Promise.resolve(null),
        ]);
        this.sessionsVolume.set(volume);
        this.inProgressProgress.set(progress && inProgress ? { sessionId: inProgress.templateSessionId!, ...progress } : null);
      } else if (current?.status === 'PendingPlanning' && current.mesocycleId) {
        await this.store.GetMesocycleDetail(current.mesocycleId);
      } else if (current?.status === 'NoActiveMacrocycle') {
        this.setup.set(await this.store.GetTrainingSetupStatus());
      }
    } finally {
      this.isLoading.set(false);
    }
  }

  weekPeriod(): string {
    const week = this.current()?.microcycle;
    return week ? formatCyclePeriod(week.startedAt, null) : '';
  }

  trainingDistributionLabel(template: MesocycleTemplate): string {
    return TRAINING_DISTRIBUTION_LABELS[template.trainingDistribution] ?? template.trainingDistribution;
  }

  async planWeek(template: MesocycleTemplate) {
    const mesocycleId = this.current()?.mesocycleId;
    if (!mesocycleId || this.planningTemplateId()) return;
    this.planningTemplateId.set(template.id);
    try {
      if (await this.store.CreateMicrocycle(mesocycleId, template.id)) {
        await this.load();
      }
    } finally {
      this.planningTemplateId.set(null);
    }
  }

  askFinishWeek() {
    this.isConfirmingFinish.set(true);
  }

  cancelFinishWeek() {
    this.isConfirmingFinish.set(false);
  }

  async finishWeek() {
    const week = this.current()?.microcycle;
    if (!week) return;
    this.isFinishing.set(true);
    try {
      if (await this.store.FinishMicrocycle(week.id)) {
        await this.load();
      }
    } finally {
      this.isFinishing.set(false);
      this.isConfirmingFinish.set(false);
    }
  }
}
