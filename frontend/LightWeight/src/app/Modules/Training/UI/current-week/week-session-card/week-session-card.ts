import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MuscleGroup, Session } from '../../../data/training-api.service';
import { VolumeBars } from '../../shared/volume-bars/volume-bars';

export type WeekSessionStatus = 'done' | 'inProgress' | 'pending';

const DONE_DAY = new Intl.DateTimeFormat('es-ES', { weekday: 'short', day: 'numeric' });

/**
 * A session of the week in progress: its state (done, in progress or pending), its sets and the groups with most
 * sets as bars. In progress, the bars show what has been done of each group over what is planned.
 * The whole card opens the session; "+N grupos más" sits above the stretched link
 */
@Component({
  selector: 'app-week-session-card',
  standalone: true,
  imports: [RouterLink, VolumeBars],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <article
      class="relative flex h-full flex-col gap-3 rounded-xl border p-4 transition duration-200 has-[a:focus-visible]:ring-2 has-[a:focus-visible]:ring-accent/50 pointer-fine:hover:-translate-y-0.5"
      [class]="status() === 'done' ? 'border-line bg-surface/50 hover:border-line-strong' : next() ? 'border-accent/55 bg-surface shadow-lg shadow-black/20' : 'border-line bg-surface shadow-lg shadow-black/20 hover:border-line-strong'"
    >
      <div class="flex items-center gap-3">
        <span
          class="flex size-9 shrink-0 items-center justify-center rounded-full border-2 font-display text-base font-bold"
          [class]="status() === 'done' ? 'border-accent bg-accent text-accent-ink' : status() === 'inProgress' ? 'border-warning text-warning' : 'border-line-strong text-subtle'"
          aria-hidden="true"
        >
          @switch (status()) {
            @case ('done') {
              <svg class="h-4 w-4" fill="none" stroke="currentColor" stroke-width="3" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="m5 13 4 4L19 7" /></svg>
            }
            @case ('inProgress') {
              <svg class="h-4 w-4" viewBox="0 0 16 16"><path d="M8 2a6 6 0 0 1 0 12z" fill="currentColor" /></svg>
            }
            @default { {{ position() }} }
          }
        </span>
        <div class="min-w-0 flex-1">
          <h4 class="break-words font-display text-2xl font-bold uppercase leading-none" [class]="status() === 'done' ? 'text-soft' : 'text-fg'">
            <a
              [routerLink]="link()"
              class="focus-visible:outline-none after:absolute after:inset-0 after:rounded-xl after:content-['']"
            >{{ session().name }}<span class="sr-only">, {{ stateLabel() }}</span></a>
          </h4>
          <p class="mt-1 text-xs text-muted">
            {{ session().exercisesCount }} {{ session().exercisesCount === 1 ? 'ejercicio' : 'ejercicios' }} · {{ stateLabel() }}
          </p>
        </div>
        @if (next()) {
          <span class="badge" [class]="status() === 'inProgress' ? 'badge-warning' : 'badge-accent'">Siguiente</span>
        }
        <svg class="h-5 w-5 shrink-0 text-subtle" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
          <path fill-rule="evenodd" d="M7.05 4.55a.75.75 0 0 1 1.06 0l5 5a.75.75 0 0 1 0 1.06l-5 5a.75.75 0 1 1-1.06-1.06L11.44 10 7.05 5.61a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd" />
        </svg>
      </div>

      <p class="flex items-baseline gap-1.5">
        @if (progress(); as progress) {
          <span class="font-display text-3xl font-bold leading-none tabular-nums text-fg">
            {{ progress.sets }}<span class="text-xl text-subtle">/{{ session().setsCount }}</span>
          </span>
          <span class="text-xs text-muted">series hechas</span>
        } @else {
          <span class="font-display text-3xl font-bold leading-none tabular-nums text-fg">{{ session().setsCount }}</span>
          <span class="text-xs text-muted">{{ session().setsCount === 1 ? 'serie' : 'series' }}</span>
        }
      </p>

      @if (volume(); as volume) {
        <app-volume-bars [volume]="volume" [done]="progress()?.byGroup ?? null" [muted]="status() === 'done'" />
      }
    </article>
  `,
})
export class WeekSessionCard {
  session = input.required<Session>();
  trainingTemplateId = input.required<string>();
  status = input.required<WeekSessionStatus>();
  /** 1-based position in the template, shown while it is pending */
  position = input.required<number>();
  /** The session that comes next: the one in progress or, without one, the first pending */
  next = input(false);
  /** When it was finished (done sessions) */
  finishedAt = input<string | null>(null);
  /** Planned sets per group; null while it loads or if it could not be loaded */
  volume = input<Partial<Record<MuscleGroup, number>> | null>(null);
  /** Only for the session in progress: planned sets done, in total and per group */
  progress = input<{ sets: number; byGroup: Partial<Record<MuscleGroup, number>> } | null>(null);

  link = computed(() => ['/training/session', this.trainingTemplateId(), this.session().id]);

  stateLabel = computed(() => {
    switch (this.status()) {
      case 'done': {
        const finishedAt = this.finishedAt();
        return finishedAt ? `hecha el ${DONE_DAY.format(new Date(finishedAt))}` : 'hecha';
      }
      case 'inProgress': return 'en curso';
      default: return 'pendiente';
    }
  });
}
