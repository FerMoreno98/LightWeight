import { Component, computed, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { OnboardingStore } from '../onboarding.store';
import { TrainingStore } from '../../../state/training.store';
import { OnboardingMinimap } from '../onboarding-minimap/onboarding-minimap';
import { nextStep, ONBOARDING_STEPS, parseOnboardingUrl, stepStates, stepUrl, STEP_LABELS } from '../onboarding-steps';

/**
 * Shell of the onboarding wizard: the minimap beside the page of the current step (the usual planning forms,
 * which move through the wizard thanks to OnboardingFlow). The setup status is reloaded after every navigation,
 * so the minimap always shows what is saved
 */
@Component({
  selector: 'app-onboarding-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, OnboardingMinimap],
  templateUrl: './onboarding-layout.html',
})
export class OnboardingLayout {
  private router = inject(Router);
  private store = inject(OnboardingStore);
  private training = inject(TrainingStore);

  status = this.store.status;
  error = this.store.error;

  private url = toSignal(
    this.router.events.pipe(filter(e => e instanceof NavigationEnd), map(() => this.router.url)),
    { initialValue: this.router.url }
  );
  location = computed(() => parseOnboardingUrl(this.url()));

  /** Compact progress for phones, where the full minimap would push the form below the fold */
  progress = computed(() => {
    const status = this.status();
    const step = this.location().step;
    if (!status || !step) return null;
    const states = stepStates(status, step);
    return {
      index: ONBOARDING_STEPS.indexOf(step) + 1,
      total: ONBOARDING_STEPS.length,
      label: STEP_LABELS[step],
      segments: ONBOARDING_STEPS.map(key => states[key]),
    };
  });

  /** Button to the next step once the current one is done (the forms do not know the wizard order) */
  next = computed(() => {
    const status = this.status();
    if (!status) return null;
    const step = nextStep(status, this.location().step);
    const url = step ? stepUrl(step, status) : null;
    return step && url ? { label: STEP_LABELS[step], url, optional: step === 'levels' } : null;
  });

  constructor() {
    // Reload the status of the program after every page and after every save of the planning
    // (creating a session or a set does not navigate, but changes what the minimap shows)
    effect(() => {
      this.url();
      this.training.planningVersion();
      this.store.refresh(this.location().programId);
    });
  }
}
