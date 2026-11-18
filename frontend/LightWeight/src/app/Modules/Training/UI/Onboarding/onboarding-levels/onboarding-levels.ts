import { Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SetupTemplate, VolumeLandmark } from '../../../data/training-api.service';
import { TrainingStore } from '../../../state/training.store';
import { VOLUME_LANDMARK_LABELS } from '../../shared/program-labels';
import { LANDMARK_TONES, VolumeGauge } from '../../shared/volume-gauge/volume-gauge';
import { OnboardingStore } from '../onboarding.store';
import { baseTemplate, LANDMARK_ORDER, LEVEL_DEFAULT_NAMES, ONBOARDING_BASE, resumeUrl } from '../onboarding-steps';

interface LevelOption {
  landmark: VolumeLandmark;
  selected: boolean;
  name: string;
}

/**
 * Wizard step: one template per volume level. Each selected landmark gets a copy of the base template
 * (same sessions and sets) that the user then adjusts. Optional: the block can be launched with only the base
 */
@Component({
  selector: 'app-onboarding-levels',
  standalone: true,
  imports: [FormsModule, RouterLink, VolumeGauge],
  templateUrl: './onboarding-levels.html',
})
export class OnboardingLevels {
  private router = inject(Router);
  private onboarding = inject(OnboardingStore);
  private training = inject(TrainingStore);

  readonly tones = LANDMARK_TONES;

  status = this.onboarding.status;
  base = computed(() => {
    const status = this.status();
    return status ? baseTemplate(status) : null;
  });

  /** Where the wizard should continue when this step cannot be done yet */
  baseStepUrl = computed(() => {
    const status = this.status();
    return status ? resumeUrl(status) : ONBOARDING_BASE;
  });

  /** Landmarks the program already has, as a string so it only changes when the templates really change */
  private presentLandmarks = computed(() => (this.status()?.templates ?? []).map(t => t.volumeLandmark).join(','));

  /** Levels the program does not have yet, from less to more volume. Reset only when the program's landmarks change */
  options = linkedSignal<LevelOption[]>(() => {
    const present = new Set(this.presentLandmarks().split(','));
    return LANDMARK_ORDER
      .filter(landmark => !present.has(landmark))
      .map(landmark => ({ landmark, selected: false, name: LEVEL_DEFAULT_NAMES[landmark] }));
  });

  /** Each level from MV to MRV with the templates the program has at it, or the option to add it */
  rungs = computed<{ landmark: VolumeLandmark; templates: SetupTemplate[]; option: LevelOption | null }[]>(() => {
    const templates = this.status()?.templates ?? [];
    const options = this.options();
    return LANDMARK_ORDER.map(landmark => ({
      landmark,
      templates: templates.filter(t => t.volumeLandmark === landmark),
      option: options.find(o => o.landmark === landmark) ?? null,
    }));
  });

  /** "Maximum Adaptive Volume" from "MAV — Maximum Adaptive Volume" */
  landmarkName(landmark: VolumeLandmark): string {
    return VOLUME_LANDMARK_LABELS[landmark].split(' — ')[1] ?? landmark;
  }

  isCreating = signal(false);
  error = signal<string | null>(null);

  selectedCount = computed(() => this.options().filter(o => o.selected).length);

  toggle(option: LevelOption) {
    this.options.update(list => list.map(o => o === option ? { ...o, selected: !o.selected } : o));
  }

  rename(option: LevelOption, name: string) {
    this.options.update(list => list.map(o => o === option ? { ...o, name } : o));
  }

  canCreate = computed(() => this.selectedCount() > 0 && this.options().every(o => !o.selected || o.name.trim().length > 0));

  /** One copy of the base template per selected level, then to the launch step */
  async createLevels() {
    const status = this.status();
    const base = this.base();
    if (!status?.programId || !base || !this.canCreate()) return;
    this.isCreating.set(true);
    this.error.set(null);
    try {
      for (const option of this.options().filter(o => o.selected)) {
        const id = await this.training.DuplicateTrainingTemplate(base.id, option.name.trim(), option.landmark);
        if (!id) {
          this.error.set(`No se ha podido crear la plantilla ${option.landmark}. Las anteriores sí se han creado.`);
          await this.onboarding.refresh(status.programId);
          return;
        }
      }
      this.router.navigate([ONBOARDING_BASE, status.programId, 'launch']);
    } finally {
      this.isCreating.set(false);
    }
  }

  skip() {
    const programId = this.status()?.programId;
    if (programId) this.router.navigate([ONBOARDING_BASE, programId, 'launch']);
  }
}
