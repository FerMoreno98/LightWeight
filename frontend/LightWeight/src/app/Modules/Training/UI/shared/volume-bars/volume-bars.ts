import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { muscleGroupLabel } from '../muscle-groups';

/** A muscle group of the bars: its planned sets and, when progress is shown, the ones already done */
export interface VolumeBar {
  group: string;
  label: string;
  sets: number;
  done: number | null;
}

/** Groups with sets, from most to fewest. `done` (when given) counts the sets already performed of each group */
export function volumeBars(volume: Partial<Record<string, number>>, done: Partial<Record<string, number>> | null = null): VolumeBar[] {
  return Object.entries(volume)
    .filter(([, sets]) => (sets ?? 0) > 0)
    .map(([group, sets]) => ({ group, label: muscleGroupLabel(group), sets: sets!, done: done ? Math.min(done[group] ?? 0, sets!) : null }))
    .sort((a, b) => b.sets - a.sets);
}

/**
 * Sets per muscle group as horizontal bars: the groups with most sets first and "+N grupos más" to unfold the rest.
 * With `done`, each bar shows the sets already performed over the planned ones ("6/10").
 * The toggle sits above a stretched card link (z-10), so it can live inside a clickable card
 */
@Component({
  selector: 'app-volume-bars',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex flex-col gap-3' },
  template: `
    @if (bars().length > 0) {
      <ul class="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-x-2.5 gap-y-1.5 text-xs" aria-label="Series por grupo muscular">
        @for (bar of visible(); track bar.group) {
          <li class="contents">
            <span class="whitespace-nowrap text-soft">{{ bar.label }}</span>
            <span class="relative h-1.5 overflow-hidden rounded-full bg-raised" aria-hidden="true">
              @if (bar.done !== null) {
                <span class="absolute inset-y-0 left-0 rounded-full opacity-30" [class]="fill()" [style.width.%]="bar.sets / max() * 100"></span>
                <span class="absolute inset-y-0 left-0 rounded-full opacity-80" [class]="fill()" [style.width.%]="bar.done / max() * 100"></span>
              } @else {
                <span class="absolute inset-y-0 left-0 rounded-full" [class]="fill()" [class.opacity-75]="!muted()" [class.opacity-40]="muted()" [style.width.%]="bar.sets / max() * 100"></span>
              }
            </span>
            <span class="whitespace-nowrap text-right font-semibold tabular-nums text-fg">
              @if (bar.done !== null) {
                {{ bar.done }}<span class="font-medium text-subtle">/{{ bar.sets }}</span>
                <span class="sr-only"> series hechas</span>
              } @else {
                {{ bar.sets }}
              }
            </span>
          </li>
        }
      </ul>
      @if (hidden() > 0) {
        <button
          type="button"
          class="relative z-10 -mb-2 -ml-2 -mt-1 min-h-11 self-start rounded-lg px-2 text-xs font-medium text-subtle transition hover:text-accent"
          [attr.aria-expanded]="showAll()"
          (click)="showAll.set(!showAll())"
        >
          {{ showAll() ? 'Ver menos' : '+' + hidden() + (hidden() === 1 ? ' grupo más' : ' grupos más') }}
        </button>
      }
    } @else {
      <p class="text-xs text-subtle">Sin series todavía</p>
    }
  `,
})
export class VolumeBars {
  /** Planned sets by muscle group (backend names: "Chest", "Back"...) */
  volume = input.required<Partial<Record<string, number>>>();
  /** Sets already performed by muscle group; null hides the progress */
  done = input<Partial<Record<string, number>> | null>(null);
  /** Background class of the bars, e.g. the colour of a volume level */
  fill = input('bg-accent');
  /** Dimmer bars, for something already finished */
  muted = input(false);
  /** Groups shown before "+N grupos más" */
  limit = input(4);

  bars = computed(() => volumeBars(this.volume(), this.done()));
  max = computed(() => this.bars()[0]?.sets ?? 0);

  showAll = signal(false);
  visible = computed(() => this.showAll() ? this.bars() : this.bars().slice(0, this.limit()));
  hidden = computed(() => Math.max(0, this.bars().length - this.limit()));
}
