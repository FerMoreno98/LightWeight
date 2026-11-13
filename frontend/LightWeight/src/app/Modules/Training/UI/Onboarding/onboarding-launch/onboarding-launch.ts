import { Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TrainingStage } from '../../../data/training-api.service';
import { TRAINING_STAGE_LABELS } from '../../shared/cycle-format';
import { OnboardingStore } from '../onboarding.store';
import { baseTemplate } from '../onboarding-steps';

/**
 * Last wizard step: starts the macrocycle, its first mesocycle (following the program) and the first week
 * (following the chosen template) in a single request
 */
@Component({
  selector: 'app-onboarding-launch',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './onboarding-launch.html',
})
export class OnboardingLaunch {
  private router = inject(Router);
  private store = inject(OnboardingStore);

  readonly stages: TrainingStage[] = ['Bulk', 'Cut', 'Maintenance'];
  readonly stageLabels = TRAINING_STAGE_LABELS;

  status = this.store.status;
  error = this.store.error;
  isLaunching = this.store.isLaunching;

  stage = signal<TrainingStage | ''>('');
  motivationLevel = signal(7);
  injuries = signal('');
  /** Template of week 1: the base one unless the user picks another */
  templateId = linkedSignal(() => {
    const status = this.status();
    return status ? baseTemplate(status)?.id ?? '' : '';
  });

  canLaunch = computed(() => !!this.status()?.canLaunch && !!this.stage() && !!this.templateId());

  async launch() {
    const status = this.status();
    const stage = this.stage();
    if (!status?.programId || !stage || !this.canLaunch()) return;
    const result = await this.store.launch({
      trainingStage: stage,
      macrocycleComments: null,
      programId: status.programId,
      motivationLevel: this.motivationLevel(),
      injuries: this.injuries().trim() || null,
      mesocycleComments: null,
      trainingTemplateId: this.templateId(),
    });
    // The home shows the week that has just started
    if (result) this.router.navigate(['/home']);
  }
}
