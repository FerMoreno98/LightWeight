import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { SharedProgram, SharedSet, SharedTemplate } from '../../../data/training-api.service';
import { PERIODIZATION_LABELS, TRAINING_DISTRIBUTION_LABELS, VOLUME_LANDMARK_LABELS } from '../../shared/program-labels';
import { TECHNIQUE_LABELS } from '../../shared/cycle-format';
import { muscleGroupLabel } from '../../shared/muscle-groups';
import { SuperSetUnit, buildUnits, groupConsecutive } from '../../shared/superset';

/** Consecutive sets of one exercise, as in the template screen */
interface ExerciseGroup {
  key: number;
  exerciseName: string;
  superSetGroupId: string | null;
  sets: SharedSet[];
}

/** A session ready to draw: its exercises on their own and its supersets (A, B...) */
interface SessionView {
  name: string;
  setsCount: number;
  units: SuperSetUnit<ExerciseGroup>[];
}

interface TemplateView {
  template: SharedTemplate;
  sessions: SessionView[];
}

type PageState = 'loading' | 'ready' | 'not-found' | 'error';

/**
 * Preview of a program shared by another user (/share/:token), read only, with a button to copy it.
 * The link only carries the share token: the backend answers 404 if it never existed or was revoked
 */
@Component({
  selector: 'app-shared-program',
  standalone: true,
  imports: [RouterLink, NgTemplateOutlet],
  templateUrl: './shared-program.html',
})
export class SharedProgramPage implements OnInit {
  private store = inject(TrainingStore);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  state = signal<PageState>('loading');
  program = signal<SharedProgram | null>(null);
  isCopying = signal(false);
  copyError = signal(false);

  private shareToken = '';

  /** Templates with their sessions grouped like the template screen (exercises in order, supersets together) */
  templates = computed<TemplateView[]>(() =>
    (this.program()?.templates ?? []).map(template => ({
      template,
      sessions: template.sessions.map(session => {
        const groups: ExerciseGroup[] = groupConsecutive(session.sets).map((run, key) => ({
          key,
          exerciseName: run.sets[0].exerciseName || 'Ejercicio eliminado',
          superSetGroupId: run.superSetGroupId,
          sets: run.sets,
        }));
        return { name: session.name, setsCount: session.sets.length, units: buildUnits(groups, g => g.superSetGroupId) };
      }),
    }))
  );

  sessionsCount = computed(() => this.templates().reduce((total, t) => total + t.sessions.length, 0));

  async ngOnInit() {
    this.shareToken = this.route.snapshot.paramMap.get('token') ?? '';
    await this.load();
  }

  async load() {
    this.state.set('loading');
    const result = await this.store.GetSharedProgram(this.shareToken);
    if (result.status === 'ok') {
      this.program.set(result.program);
      this.state.set('ready');
    } else {
      this.state.set(result.status);
    }
  }

  async copy() {
    if (this.isCopying()) return;
    this.isCopying.set(true);
    this.copyError.set(false);
    try {
      const id = await this.store.CopySharedProgram(this.shareToken);
      if (id) await this.router.navigate(['/training/trainingtemplates']);
      else this.copyError.set(true);
    } finally {
      this.isCopying.set(false);
    }
  }

  periodizationLabel(program: SharedProgram): string {
    return PERIODIZATION_LABELS[program.periodization] ?? program.periodization;
  }

  volumeLandmarkLabel(template: SharedTemplate): string {
    return VOLUME_LANDMARK_LABELS[template.volumeLandmark] ?? template.volumeLandmark;
  }

  trainingDistributionLabel(template: SharedTemplate): string {
    return TRAINING_DISTRIBUTION_LABELS[template.trainingDistribution] ?? template.trainingDistribution;
  }

  muscleLabel(group: string): string {
    return muscleGroupLabel(group);
  }

  techniqueLabel(set: SharedSet): string | null {
    return set.technique ? TECHNIQUE_LABELS[set.technique] ?? set.technique : null;
  }
}
