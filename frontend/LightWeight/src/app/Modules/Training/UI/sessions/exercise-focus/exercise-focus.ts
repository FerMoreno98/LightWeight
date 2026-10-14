import { Component, computed, effect, input, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PerformedSet } from '../../../data/training-api.service';
import { TECHNIQUE_LABELS } from '../../shared/cycle-format';
import { FocusView, SetForm, formatDay, formatPerformedSet } from '../session-workout.model';

/**
 * Focus mode of one exercise: for the set in focus it shows what was planned, what was done the previous time
 * and the form to record what is done today
 */
@Component({
  selector: 'app-exercise-focus',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './exercise-focus.html',
  styleUrl: './exercise-focus.css',
})
export class ExerciseFocus {
  view = input.required<FocusView>();
  busy = input(false);

  save = output<SetForm>();
  remove = output<PerformedSet>();
  selectSet = output<number>();
  close = output<void>();
  replaceExercise = output<void>();
  restorePlanned = output<void>();
  previousExercise = output<void>();
  nextExercise = output<void>();
  /** Another exercise of the superset, by position (A1 = 0) */
  selectMember = output<number>();

  form = signal<SetForm>({ weight: null, repetitions: null, rpe: null, isBodyWeight: false });

  slot = computed(() => this.view().slots[this.view().setIndex] ?? { planned: null, performed: null });
  /** Set of the previous time with the same position */
  previousSet = computed(() => this.view().previous?.sets[this.view().setIndex] ?? null);
  isValid = computed(() => {
    const { weight, repetitions, rpe } = this.form();
    return weight !== null && weight >= 0
      && repetitions !== null && repetitions >= 0 && Number.isInteger(repetitions)
      && rpe !== null && rpe >= 1 && rpe <= 10;
  });

  hasPerformedSets = computed(() => this.view().slots.some(s => s.performed !== null));

  /** Inputs of the "Hoy" card: the +/- buttons add `step`, the input accepts multiples of `inputStep` */
  readonly fields: { key: 'weight' | 'repetitions' | 'rpe'; label: string; step: number; inputStep: number }[] = [
    { key: 'weight', label: 'Peso (kg)', step: 2.5, inputStep: 0.25 },
    { key: 'repetitions', label: 'Reps', step: 1, inputStep: 1 },
    { key: 'rpe', label: 'RPE', step: 0.5, inputStep: 0.5 },
  ];

  readonly formatPerformedSet = formatPerformedSet;
  readonly formatDay = formatDay;

  constructor() {
    // Each time another set is focused the form is filled again: today's values if it is done,
    // otherwise the previous time's load and reps with the planned RPE
    // Only the key is tracked: reloading the workout must not overwrite what is being typed
    effect(() => {
      this.view().key;
      untracked(() => this.form.set(this.initialForm()));
    });
  }

  private initialForm(): SetForm {
    const { performed, planned } = this.slot();
    if (performed) {
      return { weight: performed.weight, repetitions: performed.repetitions, rpe: performed.rpe, isBodyWeight: performed.isBodyWeight };
    }
    const previous = this.previousSet();
    const lastToday = this.view().slots
      .slice(0, this.view().setIndex)
      .map(s => s.performed)
      .filter(p => p !== null)
      .at(-1);
    return {
      weight: previous?.weight ?? lastToday?.weight ?? null,
      repetitions: previous?.repetitions ?? planned?.repetitionRangeMax ?? lastToday?.repetitions ?? null,
      rpe: planned?.expectedRPE ?? previous?.rpe ?? null,
      isBodyWeight: previous?.isBodyWeight ?? lastToday?.isBodyWeight ?? false,
    };
  }

  update<K extends keyof SetForm>(field: K, value: SetForm[K]) {
    this.form.update(f => ({ ...f, [field]: value }));
  }

  /** Number inputs give '' while empty */
  updateNumber(field: 'weight' | 'repetitions' | 'rpe', value: number | string | null) {
    this.update(field, value === '' || value === null ? null : Number(value));
  }

  /** Adds a step to a field from the +/- buttons */
  step(field: 'weight' | 'repetitions' | 'rpe', delta: number) {
    const current = this.form()[field] ?? 0;
    const min = field === 'rpe' ? 1 : 0;
    const max = field === 'rpe' ? 10 : Number.MAX_SAFE_INTEGER;
    this.update(field, Math.min(max, Math.max(min, Math.round((current + delta) * 100) / 100)));
  }

  submit() {
    if (this.isValid() && !this.busy() && !this.view().readOnly) this.save.emit(this.form());
  }

  techniqueLabel(technique: string | null): string | null {
    return technique ? TECHNIQUE_LABELS[technique as keyof typeof TECHNIQUE_LABELS] ?? technique : null;
  }
}
