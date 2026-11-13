import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TrainingApiService } from '../../data/training-api.service';
import { resumeUrl } from './onboarding-steps';

/** /training/onboarding on its own: goes to the step where the user left the wizard (to the home if already training) */
export const resumeOnboardingGuard: CanActivateFn = async () => {
  const router = inject(Router);
  const api = inject(TrainingApiService);
  try {
    const status = await firstValueFrom(api.GetTrainingSetupStatus());
    return router.parseUrl(resumeUrl(status));
  } catch {
    return router.parseUrl('/home');
  }
};
