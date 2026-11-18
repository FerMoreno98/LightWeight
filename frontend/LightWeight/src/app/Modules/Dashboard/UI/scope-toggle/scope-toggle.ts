import { Component, input, output } from '@angular/core';
import { ProgressScope } from '../../state/exercise-progress.store';

/** "Macrociclo actual / Histórico" switch of a Progreso filter. Disabled (showing the history) without an active macrocycle */
@Component({
    selector: 'app-scope-toggle',
    standalone: true,
    template: `
      <div class="flex items-center justify-between gap-3 sm:justify-start">
        <span class="w-20 shrink-0 text-xs font-medium text-soft">{{ label() }}</span>
        <div class="flex gap-1 rounded-xl border border-line bg-canvas/60 p-1" role="group" [attr.aria-label]="label() + ': periodo'">
          @for (option of options; track option.value) {
            <button
              type="button"
              (click)="changed.emit(option.value)"
              [disabled]="disabled()"
              [attr.aria-pressed]="value() === option.value"
              class="rounded-lg px-3 py-1 text-xs font-medium transition disabled:cursor-not-allowed disabled:opacity-50"
              [class]="value() === option.value ? 'bg-accent text-accent-ink' : 'text-soft hover:bg-surface'"
            >{{ option.label }}</button>
          }
        </div>
      </div>
    `,
})
export class ScopeToggle {
    label = input.required<string>();
    value = input.required<ProgressScope>();
    disabled = input(false);
    changed = output<ProgressScope>();

    readonly options: { value: ProgressScope; label: string }[] = [
        { value: 'macrocycle', label: 'Macrociclo actual' },
        { value: 'history', label: 'Histórico' },
    ];
}
