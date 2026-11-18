import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { SetRow, Trend } from '../session-workout.model';

export type SetField = 'weight' | 'repetitions' | 'rpe';

/** How a row is shown: its values (or the grey hints while empty) and whether it is done and saved */
export interface RowView {
  done: boolean;
  /** saving: sent, waiting for the server · unsaved: it failed and will be retried */
  status: 'saving' | 'unsaved' | null;
  weight: string;
  repetitions: string;
  rpe: string;
  /** Which values are hints (the previous time, the target) and not typed or saved ones */
  hint: Record<SetField, boolean>;
  previous: string;
  /** Only in a finished session: what was done today against the previous time */
  comparison?: { today: string; rpe: string | null; change: string; trend: Trend | 'skipped' | null };
}

/** A line of the table: a set, or a divider (a new target, a round of a superset, the extra sets) */
export type TableEntry =
  | { kind: 'divider'; key: string; label: string; tone: 'target' | 'round' | 'extra' }
  | { kind: 'row'; key: string; row: SetRow; view: RowView };

/**
 * Sets of an exercise (or of the rounds of a superset) as a table: number, previous time, kg, reps, RPE and ✓.
 * Tapping a value opens the set; ✓ marks it done (or undoes it). It only shows and emits: the page saves
 */
@Component({
  selector: 'app-set-table',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    @if (mode() === 'compare') {
      <!-- Finished session: the previous time against today, set by set -->
      <table class="w-full table-fixed border-collapse text-sm tabular-nums">
        <colgroup><col class="w-9" /><col class="w-[27%]" /><col class="w-[31%]" /><col /></colgroup>
        <thead>
          <tr class="text-[10px] font-semibold uppercase tracking-wider text-subtle">
            <th class="pb-1 pl-1 text-left font-semibold">#</th>
            <th class="pb-1 text-left font-semibold">Anterior</th>
            <th class="pb-1 text-left font-semibold">Hoy</th>
            <th class="pb-1 pr-1 text-right font-semibold">Cambio</th>
          </tr>
        </thead>
        <tbody>
          @for (entry of entries(); track entry.key; let first = $first) {
            @if (entry.kind === 'divider') {
              <tr>
                <td colspan="4"
                    class="px-1 pb-0.5 text-[11px] font-semibold tracking-wide"
                    [class]="(first ? 'pt-0.5 ' : 'border-t border-line pt-2 ') + (entry.tone === 'round' ? 'text-info' : entry.tone === 'extra' ? 'text-subtle' : 'text-accent')">
                  {{ entry.label }}
                </td>
              </tr>
            } @else {
              @let row = entry.row;
              @let cmp = entry.view.comparison;
              <tr class="border-t border-line/50 first:border-t-0">
                <td class="py-2 pl-1 text-xs font-bold text-subtle">{{ row.label ?? row.position }}</td>
                <td class="truncate py-2 text-subtle">{{ entry.view.previous }}</td>
                <td class="py-2" [class]="cmp?.trend === 'skipped' ? 'italic text-subtle' : 'font-semibold text-fg'">
                  {{ cmp?.today ?? '—' }}
                  @if (cmp?.rpe) {
                    <span class="ml-0.5 text-xs font-normal text-muted">&#64;{{ cmp!.rpe }}</span>
                  }
                </td>
                <td class="py-2 pr-1 text-right text-xs font-semibold" [class]="trendClass(cmp?.trend ?? null)">
                  @switch (cmp?.trend) {
                    @case ('up') { <span aria-hidden="true">↑ </span><span class="sr-only">Mejor: </span> }
                    @case ('down') { <span aria-hidden="true">↓ </span><span class="sr-only">Peor: </span> }
                  }
                  {{ cmp?.change ?? '' }}
                </td>
              </tr>
            }
          }
        </tbody>
      </table>
    } @else {
    <table class="w-full table-fixed border-collapse text-sm tabular-nums">
      <colgroup>
        <col class="w-9" /><col class="w-[22%]" /><col /><col /><col /><col class="w-12" />
      </colgroup>
      <thead>
        <tr class="text-[10px] font-semibold uppercase tracking-wider text-subtle">
          <th class="pb-1 pl-1 text-left font-semibold">#</th>
          <th class="pb-1 font-semibold">Anterior</th>
          <th class="pb-1 font-semibold">kg</th>
          <th class="pb-1 font-semibold">Reps</th>
          <th class="pb-1 font-semibold">RPE</th>
          <th class="pb-1"><span class="sr-only">Hecha</span></th>
        </tr>
      </thead>
      <tbody>
        @for (entry of entries(); track entry.key; let first = $first) {
          @if (entry.kind === 'divider') {
            <tr>
              <td colspan="6"
                  class="px-1 pb-0.5 text-[11px] font-semibold tracking-wide"
                  [class]="(first ? 'pt-0.5 ' : 'border-t border-line pt-2 ') + (entry.tone === 'round' ? 'text-info' : entry.tone === 'extra' ? 'text-subtle' : 'text-accent')">
                {{ entry.label }}
              </td>
            </tr>
          } @else {
            @let row = entry.row;
            @let view = entry.view;
            <tr>
              <td class="py-0.5 pl-1 text-xs font-bold text-subtle">{{ row.label ?? row.position }}</td>
              <td class="truncate py-0.5 text-center text-xs text-subtle">{{ view.previous }}</td>
              @for (field of fields; track field) {
                <td class="px-0.5 py-0.5">
                  @if (editable()) {
                    <button
                      type="button"
                      class="h-10 w-full rounded-lg border px-0.5 text-center transition-colors"
                      [class]="cellClass(view, field)"
                      (click)="open.emit({ row, field })"
                      [attr.aria-label]="fieldLabel(field) + ' de la serie ' + row.position + ' de ' + row.exerciseName + ': ' + valueOf(view, field)"
                    >{{ valueOf(view, field) }}</button>
                  } @else {
                    <span class="flex h-10 items-center justify-center rounded-lg" [class]="cellClass(view, field)">{{ valueOf(view, field) }}</span>
                  }
                </td>
              }
              <td class="py-0.5 text-right">
                <button
                  type="button"
                  class="inline-flex size-10 items-center justify-center rounded-lg transition-colors disabled:cursor-default"
                  [class]="checkClass(view)"
                  [disabled]="!editable() || view.status === 'saving'"
                  [attr.aria-pressed]="view.done"
                  [attr.aria-label]="(view.done ? 'Desmarcar' : 'Marcar hecha') + ' la serie ' + row.position + ' de ' + row.exerciseName + (view.status === 'unsaved' ? ' (sin guardar)' : '')"
                  (click)="toggle.emit(row)"
                >
                  <svg class="h-4 w-4" fill="none" stroke="currentColor" stroke-width="3" viewBox="0 0 24 24" aria-hidden="true">
                    <path stroke-linecap="round" stroke-linejoin="round" d="m5 13 4 4L19 7" />
                  </svg>
                </button>
              </td>
            </tr>
          }
        }
      </tbody>
    </table>
    }
  `,
})
export class SetTable {
  entries = input.required<TableEntry[]>();
  /** Sets can be typed and marked (the session is in progress) */
  editable = input(false);
  /** record: the sets to type and mark · compare: a finished session against the previous time */
  mode = input<'record' | 'compare'>('record');

  toggle = output<SetRow>();
  open = output<{ row: SetRow; field: SetField }>();

  readonly fields: SetField[] = ['weight', 'repetitions', 'rpe'];

  valueOf(view: RowView, field: SetField): string {
    return view[field];
  }

  fieldLabel(field: SetField): string {
    return field === 'weight' ? 'Peso' : field === 'repetitions' ? 'Repeticiones' : 'RPE';
  }

  cellClass(view: RowView, field: SetField): string {
    const text = view.hint[field] ? 'text-xs font-medium text-subtle' : 'text-[15px] font-semibold text-fg';
    if (view.done) {
      return `${text} border-transparent ${view.status === 'unsaved' ? 'bg-warning/10' : 'bg-accent/12'}`;
    }
    return `${text} border-line-strong bg-canvas ${this.editable() ? 'hover:border-accent/60' : ''}`;
  }

  trendClass(trend: Trend | 'skipped' | null): string {
    return trend === 'up' ? 'text-accent' : trend === 'down' ? 'text-danger' : 'text-subtle';
  }

  checkClass(view: RowView): string {
    if (!view.done) return 'bg-raised text-subtle enabled:hover:text-fg';
    if (view.status === 'unsaved') return 'bg-warning text-accent-ink';
    return `bg-accent text-accent-ink ${view.status === 'saving' ? 'opacity-60' : ''}`;
  }
}
