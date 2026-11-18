import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { VolumeLandmark } from '../../../data/training-api.service';
import { VOLUME_LANDMARK_LABELS } from '../program-labels';

/** From less to more volume: the order of the gauge and of the templates of a program */
export const VOLUME_LANDMARK_ORDER: readonly VolumeLandmark[] = ['MV', 'MEV', 'MAV', 'MRV'];

/**
 * Colour of each level, the same scale everywhere: it heats up with the volume, MV (deload) is the cool one.
 * Full class names so Tailwind finds them
 */
export const LANDMARK_TONES: Record<VolumeLandmark, { text: string; fill: string; soft: string; border: string }> = {
  MV: { text: 'text-info', fill: 'bg-info', soft: 'bg-info/15', border: 'border-info' },
  MEV: { text: 'text-accent', fill: 'bg-accent', soft: 'bg-accent/15', border: 'border-accent' },
  MAV: { text: 'text-warning', fill: 'bg-warning', soft: 'bg-warning/15', border: 'border-warning' },
  MRV: { text: 'text-danger', fill: 'bg-danger', soft: 'bg-danger/15', border: 'border-danger' },
};

/**
 * Four-step gauge MV → MRV filled up to the given level, so the level reads without knowing the acronyms.
 * Announced as "Nivel de volumen: MAV — Maximum Adaptive Volume"
 */
@Component({
  selector: 'app-volume-gauge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block', role: 'img', '[attr.aria-label]': 'label()' },
  template: `
    <div class="grid grid-cols-4 gap-0.75" aria-hidden="true">
      @for (step of steps(); track step.landmark) {
        <span class="h-1.5 rounded-full" [class]="step.on ? tone().fill : 'bg-raised'"></span>
      }
    </div>
    @if (showLabels()) {
      <div class="mt-1 grid grid-cols-4 gap-0.75 text-[10px] font-semibold tracking-wide" aria-hidden="true">
        @for (step of steps(); track step.landmark) {
          <span [class]="step.current ? tone().text : 'text-subtle'">{{ step.landmark }}</span>
        }
      </div>
    }
  `,
})
export class VolumeGauge {
  landmark = input.required<VolumeLandmark>();
  /** Writes MV · MEV · MAV · MRV under the steps */
  showLabels = input(true);

  tone = computed(() => LANDMARK_TONES[this.landmark()]);
  label = computed(() => `Nivel de volumen: ${VOLUME_LANDMARK_LABELS[this.landmark()]}`);

  steps = computed(() => {
    const index = VOLUME_LANDMARK_ORDER.indexOf(this.landmark());
    return VOLUME_LANDMARK_ORDER.map((landmark, i) => ({ landmark, on: i <= index, current: i === index }));
  });
}
