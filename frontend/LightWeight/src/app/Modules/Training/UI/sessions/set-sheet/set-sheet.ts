import { ChangeDetectionStrategy, Component, ElementRef, afterNextRender, computed, effect, inject, Injector, input, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DraftValues, SetRow, completeValues, formatShortSet, targetLabel } from '../session-workout.model';
import { SetField } from '../set-table/set-table';

interface SheetField {
  key: SetField;
  label: string;
  step: number;
  min: number;
  max: number;
  inputMode: 'decimal' | 'numeric';
}

const FIELDS: SheetField[] = [
  { key: 'weight', label: 'Peso (kg)', step: 2.5, min: 0, max: 9999, inputMode: 'decimal' },
  { key: 'repetitions', label: 'Repeticiones', step: 1, min: 0, max: 999, inputMode: 'numeric' },
  { key: 'rpe', label: 'RPE', step: 0.5, min: 1, max: 10, inputMode: 'decimal' },
];

/**
 * Sheet of one set: weight, reps and RPE together, each one typed with the keyboard or adjusted with big −/+,
 * with shortcuts (the previous time, +2,5 / +5 kg, body weight). Closing it keeps what was typed as a draft
 */
@Component({
  selector: 'app-set-sheet',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown.escape)': 'dismiss()' },
  template: `
    @if (row(); as row) {
      <div class="fixed inset-0 z-50 flex items-end justify-center sm:items-center sm:px-4">
        <div class="backdrop-fade absolute inset-0 bg-black/60" (click)="dismiss()"></div>
        <form
          class="dialog-pop relative flex max-h-[92vh] w-full flex-col gap-4 overflow-y-auto rounded-t-2xl border border-line bg-surface px-4 pt-3 pb-[calc(1rem+env(safe-area-inset-bottom,0px))] shadow-2xl shadow-black/50 sm:max-w-md sm:rounded-2xl sm:pb-5"
          role="dialog"
          aria-modal="true"
          aria-labelledby="set-sheet-title"
          (ngSubmit)="submit()"
        >
          <span class="mx-auto h-1 w-10 rounded-full bg-line-strong sm:hidden" aria-hidden="true"></span>
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0">
              <p class="eyebrow">Serie {{ row.position }}{{ row.planned ? '' : ' · extra' }}</p>
              <h2 id="set-sheet-title" class="mt-0.5 break-words font-display text-2xl font-bold uppercase leading-tight text-fg">{{ row.exerciseName }}</h2>
            </div>
            <button type="button" class="btn btn-ghost btn-sm -mr-2" (click)="dismiss()">Cerrar</button>
          </div>

          <div class="flex flex-wrap gap-1.5">
            @if (row.target; as target) {
              <span class="badge badge-accent">Objetivo {{ targetText(row) }}</span>
            } @else {
              <span class="badge badge-neutral">Fuera del plan</span>
            }
            @if (row.previous; as previous) {
              <span class="badge badge-neutral">La otra vez {{ previousText(row) }}</span>
            }
          </div>

          @for (field of fields; track field.key) {
            <div>
              <label [for]="'set-sheet-' + field.key" class="mb-1 flex items-baseline justify-between text-xs font-semibold text-muted">
                {{ field.label }}
                <span class="font-normal text-subtle">± {{ format(field.step) }}</span>
              </label>
              <div class="grid grid-cols-[3.5rem_minmax(0,1fr)_3.5rem] gap-2">
                <button type="button" class="flex h-14 items-center justify-center rounded-lg border border-line bg-raised text-2xl text-fg transition-colors hover:border-line-strong active:bg-line"
                        (click)="step(field, -1)" [attr.aria-label]="'Restar ' + format(field.step) + ' a ' + field.label">−</button>
                <input
                  [id]="'set-sheet-' + field.key"
                  type="number"
                  [attr.inputmode]="field.inputMode"
                  [step]="field.key === 'repetitions' ? 1 : 0.25"
                  [min]="field.min"
                  [max]="field.max"
                  [name]="field.key"
                  [ngModel]="values()[field.key] ?? null"
                  (ngModelChange)="set(field.key, $event)"
                  class="h-14 w-full min-w-0 rounded-lg border border-line-strong bg-canvas px-2 text-center font-display text-4xl font-bold text-fg [appearance:textfield] focus:border-accent focus:ring-2 focus:ring-accent/25 focus:outline-none [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
                />
                <button type="button" class="flex h-14 items-center justify-center rounded-lg border border-line bg-raised text-2xl text-fg transition-colors hover:border-line-strong active:bg-line"
                        (click)="step(field, 1)" [attr.aria-label]="'Sumar ' + format(field.step) + ' a ' + field.label">+</button>
              </div>
            </div>
          }

          <div class="flex flex-wrap gap-2">
            @if (row.previous) {
              <button type="button" class="min-h-9 rounded-full border border-line-strong px-3 text-xs font-semibold text-soft transition hover:border-accent hover:text-fg" (click)="sameAsPrevious()">Como la otra vez</button>
            }
            <button type="button" class="min-h-9 rounded-full border border-line-strong px-3 text-xs font-semibold text-soft transition hover:border-accent hover:text-fg" (click)="addWeight(2.5)">+2,5 kg</button>
            <button type="button" class="min-h-9 rounded-full border border-line-strong px-3 text-xs font-semibold text-soft transition hover:border-accent hover:text-fg" (click)="addWeight(5)">+5 kg</button>
          </div>

          <label class="inline-flex min-h-9 items-center gap-2 text-sm text-soft">
            <input type="checkbox" name="isBodyWeight" class="h-4 w-4 rounded border-line-strong bg-canvas accent-accent"
                   [ngModel]="values().isBodyWeight ?? false" (ngModelChange)="setBodyWeight($event)" />
            Peso corporal <span class="text-xs text-subtle">(el peso es lastre extra)</span>
          </label>

          @if (showErrors() && !valid()) {
            <p class="text-sm text-danger" role="alert">Faltan datos: el peso, unas repeticiones enteras y un RPE entre 1 y 10.</p>
          }

          <button type="submit" class="btn btn-primary min-h-13 w-full">
            {{ done() ? 'Guardar cambios' : 'Marcar hecha' }}
          </button>
        </form>
      </div>
    }
  `,
})
export class SetSheet {
  /** Set being edited; null closes the sheet */
  row = input<SetRow | null>(null);
  /** Values it opens with: what is saved or typed, completed with the hints */
  initial = input<DraftValues>({});
  /** Field focused when it opens */
  focusField = input<SetField>('weight');
  /** The set is already done: the button saves the correction */
  done = input(false);

  save = output<DraftValues>();
  /** Closed without saving: what was typed stays as a draft */
  closed = output<DraftValues>();

  readonly fields = FIELDS;
  private host = inject(ElementRef<HTMLElement>);
  private injector = inject(Injector);

  values = signal<DraftValues>({});
  showErrors = signal(false);
  valid = computed(() => completeValues(this.values()) !== null);

  constructor() {
    // Filled again each time another set is opened, and the chosen field is focused
    effect(() => {
      const row = this.row();
      if (!row) return;
      untracked(() => {
        this.values.set({ ...this.initial() });
        this.showErrors.set(false);
        const field = this.focusField();
        afterNextRender(() => {
          const input = this.host.nativeElement.querySelector(`#set-sheet-${field}`) as HTMLInputElement | null;
          input?.focus();
          input?.select();
        }, { injector: this.injector });
      });
    });
  }

  set(field: SetField, value: number | string | null) {
    const number = value === '' || value === null ? undefined : Number(value);
    this.values.update(v => ({ ...v, [field]: Number.isFinite(number) ? number : undefined }));
  }

  step(field: SheetField, direction: 1 | -1) {
    const current = this.values()[field.key] ?? (field.key === 'rpe' ? 8 : 0);
    const next = Math.round((current + direction * field.step) * 100) / 100;
    this.values.update(v => ({ ...v, [field.key]: Math.min(field.max, Math.max(field.min, next)) }));
  }

  addWeight(amount: number) {
    this.values.update(v => ({ ...v, weight: Math.round(((v.weight ?? 0) + amount) * 100) / 100 }));
  }

  setBodyWeight(isBodyWeight: boolean) {
    this.values.update(v => ({ ...v, isBodyWeight }));
  }

  sameAsPrevious() {
    const previous = this.row()?.previous;
    if (previous) this.values.update(v => ({ ...v, weight: previous.weight, repetitions: previous.repetitions, isBodyWeight: previous.isBodyWeight }));
  }

  submit() {
    if (!this.valid()) {
      this.showErrors.set(true);
      return;
    }
    this.save.emit(this.values());
  }

  dismiss() {
    if (this.row()) this.closed.emit(this.values());
  }

  targetText(row: SetRow): string {
    return row.target ? targetLabel(row.target) : '';
  }

  previousText(row: SetRow): string {
    return row.previous ? formatShortSet(row.previous) : '';
  }

  format(step: number): string {
    return step.toLocaleString('es-ES');
  }
}
