import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TrainingSetupStatus } from '../../../data/training-api.service';
import { TrainingStore } from '../../../state/training.store';
import { TrainingMap } from '../training-map/training-map';
import { ONBOARDING_BASE } from '../onboarding-steps';

/**
 * Entry of the onboarding wizard and the "how does it work" page of the training section: the animated map.
 * The button adapts to where the user is: start the wizard, resume it, or go back for someone already training
 */
@Component({
  selector: 'app-onboarding-intro',
  standalone: true,
  imports: [TrainingMap, RouterLink],
  templateUrl: './onboarding-intro.html',
})
export class OnboardingIntro {
  private router = inject(Router);
  private store = inject(TrainingStore);

  readonly wizardUrl = ONBOARDING_BASE;
  status = signal<TrainingSetupStatus | null>(null);

  isTraining = computed(() => this.status()?.step === 'Done');
  ctaLabel = computed(() => {
    const status = this.status();
    if (status?.step === 'Done') return 'Volver a mi entrenamiento';
    return status?.programId ? 'Seguir con el asistente' : 'Diseñar mi primer programa';
  });

  async ngOnInit() {
    this.status.set(await this.store.GetTrainingSetupStatus());
  }

  start() {
    this.router.navigateByUrl(this.isTraining() ? '/home' : ONBOARDING_BASE);
  }
}
