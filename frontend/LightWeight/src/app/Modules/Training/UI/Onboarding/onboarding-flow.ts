import { Injectable } from '@angular/core';
import { UrlTree } from '@angular/router';
import { TrainingFlow } from '../shared/training-flow';
import { ONBOARDING_BASE, parseOnboardingUrl } from './onboarding-steps';

/**
 * TrainingFlow of the onboarding wizard: the same planning forms move through the wizard pages.
 * The program comes from the current URL (/training/onboarding/:programId/...), so the forms do not need to know it
 */
@Injectable()
export class OnboardingFlow extends TrainingFlow {
  override readonly embedded = true;

  override createProgram(): UrlTree {
    return this.router.createUrlTree([ONBOARDING_BASE, 'program']);
  }

  override programCreated(programId: string): UrlTree {
    return this.createTemplate(programId);
  }

  override createTemplate(programId?: string): UrlTree {
    return this.inProgram(programId, 'template');
  }

  /** Leaving a form without a previous page goes back to where the wizard was left */
  override templatesList(): UrlTree {
    return this.router.createUrlTree([ONBOARDING_BASE]);
  }

  override templateSessions(templateId: string): UrlTree {
    return this.inProgram(undefined, 'sessions', templateId);
  }

  override sessionSets(templateId: string, sessionId: string): UrlTree {
    return this.inProgram(undefined, 'sets', templateId, sessionId);
  }

  override exerciseSettings(templateId: string, sessionId: string, setId?: string): UrlTree {
    const tree = this.inProgram(undefined, 'exercise', templateId, sessionId);
    if (setId) tree.queryParams = { setId };
    return tree;
  }

  /** A page of the program of the current URL (or the given one). Without program it resumes the wizard */
  private inProgram(programId: string | undefined, ...page: string[]): UrlTree {
    const id = programId ?? parseOnboardingUrl(this.router.url).programId;
    return id
      ? this.router.createUrlTree([ONBOARDING_BASE, id, ...page])
      : this.router.createUrlTree([ONBOARDING_BASE]);
  }
}
