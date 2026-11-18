import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, output, signal } from '@angular/core';

/** Rest between sets until the plan can say how long each exercise rests */
export const DEFAULT_REST_SECONDS = 120;
/** After this long past the rest the bar goes away on its own */
const OVERTIME_LIMIT_SECONDS = 5 * 60;

/** A rest that has started: when, and after which set */
export interface RestStart {
  at: number;
  label: string;
}

/**
 * Countdown of the rest after marking a set, fixed at the bottom: ±15 s and "Saltar". When it reaches zero it keeps
 * counting the extra time, so you know how long you have been resting
 */
@Component({
  selector: 'app-rest-timer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (start() && visible()) {
      <!-- On phones it sits right above the tab bar (4rem), so the navigation stays usable while resting -->
      <div class="fixed inset-x-0 bottom-[calc(4rem+env(safe-area-inset-bottom,0px))] z-30 border-t border-line bg-raised/95 md:bottom-0 md:pb-[env(safe-area-inset-bottom,0px)] pl-[max(1rem,env(safe-area-inset-left,0px))] pr-[max(1rem,env(safe-area-inset-right,0px))] shadow-[0_-12px_30px_rgba(0,0,0,0.4)] backdrop-blur"
           role="timer" aria-label="Descanso">
        <div class="mx-auto flex max-w-3xl items-center gap-3 py-2.5">
          <span class="min-w-[4.5rem] font-display text-3xl font-bold leading-none tabular-nums" [class]="left() < 0 ? 'text-accent' : 'text-fg'">{{ clock() }}</span>
          <p class="min-w-0 flex-1 text-xs leading-tight text-muted">
            <span class="block text-sm font-semibold text-fg">{{ left() < 0 ? '¡A por la siguiente!' : 'Descanso' }}</span>
            <span class="block truncate">{{ start()!.label }}</span>
          </p>
          <button type="button" class="btn btn-ghost btn-sm px-2" (click)="adjust(-15)" aria-label="Quitar 15 segundos al descanso">−15</button>
          <button type="button" class="btn btn-ghost btn-sm px-2" (click)="adjust(15)" aria-label="Añadir 15 segundos al descanso">+15</button>
          <button type="button" class="btn btn-secondary btn-sm" (click)="skip()">Saltar</button>
        </div>
      </div>
    }
  `,
})
export class RestTimer {
  /** A new value starts a new rest */
  start = input<RestStart | null>(null);
  seconds = input(DEFAULT_REST_SECONDS);

  dismissed = output<void>();

  private now = signal(Date.now());
  private extra = signal(0);
  private hidden = signal(false);

  left = computed(() => {
    const start = this.start();
    if (!start) return 0;
    return Math.round((start.at - this.now()) / 1000) + this.seconds() + this.extra();
  });
  visible = computed(() => !this.hidden() && this.left() > -OVERTIME_LIMIT_SECONDS);

  clock = computed(() => {
    const left = this.left();
    const abs = Math.abs(left);
    return `${left < 0 ? '+' : ''}${Math.floor(abs / 60)}:${String(abs % 60).padStart(2, '0')}`;
  });

  constructor() {
    // Every new rest starts from zero adjustments
    effect(() => {
      this.start();
      this.extra.set(0);
      this.hidden.set(false);
      this.now.set(Date.now());
    });
    const timer = setInterval(() => { if (this.start()) this.now.set(Date.now()); }, 1000);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  adjust(seconds: number) {
    this.extra.update(e => e + seconds);
  }

  skip() {
    this.hidden.set(true);
    this.dismissed.emit();
  }
}
