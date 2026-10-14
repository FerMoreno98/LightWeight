import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TrainingStore } from '../../state/training.store';
import { MesocycleTemplate, Session } from '../../data/training-api.service';
import { TRAINING_DISTRIBUTION_LABELS, formatCyclePeriod } from '../shared/cycle-format';
import { ConfirmDialog } from '../../../../Shared/confirm-dialog/confirm-dialog';

/**
 * Week the user is training now: the active week of the active mesocycle of the active macrocycle.
 * When some level is missing it shows the step to take (start a macrocycle, a mesocycle or plan the week)
 */
@Component({
  selector: 'app-current-week',
  standalone: true,
  imports: [RouterLink, ConfirmDialog],
  templateUrl: './current-week.html',
  styleUrl: './current-week.css',
})
export class CurrentWeek {
  private store = inject(TrainingStore);
  private router = inject(Router);

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

  /** Own loading flag: the store one is shared by every request of the page */
  isLoading = signal(true);
  isFinishing = signal(false);
  isConfirmingFinish = signal(false);
  /** Template being planned as the next week, to disable the cards and avoid planning it twice */
  planningTemplateId = signal<string | null>(null);

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
        await this.store.GetSessionsFromATrainingTemplate(current.microcycle.trainingTemplateId);
      } else if (current?.status === 'PendingPlanning' && current.mesocycleId) {
        await this.store.GetMesocycleDetail(current.mesocycleId);
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

  /** "Hecha" / "En curso" for the sessions already started this week */
  sessionStatus(session: Session): 'done' | 'inProgress' | null {
    const started = this.current()?.trainingSessions.find(s => s.templateSessionId === session.id);
    if (!started) return null;
    return started.finishedAt ? 'done' : 'inProgress';
  }

  openSession(session: Session) {
    const templateId = this.current()?.microcycle?.trainingTemplateId;
    if (!templateId) return;
    this.router.navigate(['/training/session', templateId, session.id]);
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
