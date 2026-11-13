import { inject, Injectable } from '@angular/core';
import { Router, UrlTree } from '@angular/router';

/**
 * Where the planning forms (program, template, sessions, sets) go next.
 *
 * The forms never build their destination URLs themselves: they ask this service, and use the UrlTree it returns
 * with router.navigateByUrl() or [routerLink]. This default implementation keeps the usual routes of the app.
 * The onboarding wizard provides its own subclass on its route ({ provide: TrainingFlow, useClass: ... }),
 * so the very same forms move through the wizard steps instead
 */
@Injectable({ providedIn: 'root' })
export class TrainingFlow {
  protected router = inject(Router);

  /** True when the forms are shown inside a wizard: they can hide their own header and back links */
  readonly embedded: boolean = false;

  /** Form to create a program */
  createProgram(): UrlTree {
    return this.router.createUrlTree(['/training/createprogram']);
  }

  /** Next step once a program has been created: its first template */
  programCreated(programId: string): UrlTree {
    return this.createTemplate(programId);
  }

  /** Form to create a template, with the program preselected when given */
  createTemplate(programId?: string): UrlTree {
    return this.router.createUrlTree(['/training/trainingtemplate'], programId ? { queryParams: { programId } } : {});
  }

  /** List of programs and templates: fallback when a form is left without a previous page */
  templatesList(): UrlTree {
    return this.router.createUrlTree(['/training/trainingtemplates']);
  }

  /** Sessions of a template (also the next step once the template has been created) */
  templateSessions(templateId: string): UrlTree {
    return this.router.createUrlTree(['/training/sessiontemplate', templateId]);
  }

  /** Planned sets of a session (also where the exercise form returns) */
  sessionSets(templateId: string, sessionId: string): UrlTree {
    return this.router.createUrlTree(['/training/sessionsets', templateId, sessionId]);
  }

  /** Form to add a set to a session, or to edit one when setId is given */
  exerciseSettings(templateId: string, sessionId: string, setId?: string): UrlTree {
    return this.router.createUrlTree(
      ['/training/exercisesettings', templateId, sessionId],
      setId ? { queryParams: { setId } } : {}
    );
  }
}
