import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { Set } from '../../../data/training-api.service';
import { TECHNIQUE_LABELS } from '../../shared/cycle-format';

/** Consecutive sets of the same exercise, in the order they were planned */
interface ExerciseBlock {
  exerciseId: string;
  exerciseName: string;
  sets: Set[];
}

/** Plan of a session of the current week: its exercises and the sets to perform */
@Component({
  selector: 'app-session-plan',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './session-plan.html',
  styleUrl: './session-plan.css',
})
export class SessionPlan {
  private store = inject(TrainingStore);
  private route = inject(ActivatedRoute);

  error = this.store.error;
  isLoading = signal(true);
  private sessionId = '';

  session = computed(() => this.store.sessions().find(s => s.id === this.sessionId) ?? null);

  blocks = computed<ExerciseBlock[]>(() => {
    const blocks: ExerciseBlock[] = [];
    for (const set of this.store.sets()) {
      const last = blocks.at(-1);
      if (last && last.exerciseId === set.exerciseId) {
        last.sets.push(set);
      } else {
        blocks.push({ exerciseId: set.exerciseId, exerciseName: set.exerciseName || 'Ejercicio eliminado', sets: [set] });
      }
    }
    return blocks;
  });

  async ngOnInit() {
    const templateId = this.route.snapshot.paramMap.get('templateid')!;
    this.sessionId = this.route.snapshot.paramMap.get('sessionid')!;
    try {
      // The sessions are reloaded too so the name is there when the page is opened directly
      await Promise.all([
        this.store.GetSetsFromASessionTemplate(templateId, this.sessionId),
        this.session() ? Promise.resolve(true) : this.store.GetSessionsFromATrainingTemplate(templateId),
      ]);
    } finally {
      this.isLoading.set(false);
    }
  }

  techniqueLabel(set: Set): string | null {
    return set.technique ? TECHNIQUE_LABELS[set.technique] ?? set.technique : null;
  }
}
