import { Injectable } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { TrainingFlow } from './training-flow';

describe('TrainingFlow', () => {
  let flow: TrainingFlow;
  let router: Router;
  const url = (tree: UrlTree) => router.serializeUrl(tree);

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    flow = TestBed.inject(TrainingFlow);
    router = TestBed.inject(Router);
  });

  // The default destinations are the routes the forms used to build by hand: changing one changes the app
  it('keeps the usual routes of the planning forms', () => {
    expect(url(flow.createProgram())).toBe('/training/createprogram');
    expect(url(flow.programCreated('p1'))).toBe('/training/trainingtemplate?programId=p1');
    expect(url(flow.createTemplate())).toBe('/training/trainingtemplate');
    expect(url(flow.templatesList())).toBe('/training/trainingtemplates');
    expect(url(flow.templateSessions('t1'))).toBe('/training/sessiontemplate/t1');
    expect(url(flow.sessionSets('t1', 's1'))).toBe('/training/sessionsets/t1/s1');
    expect(url(flow.exerciseSettings('t1', 's1'))).toBe('/training/exercisesettings/t1/s1');
    expect(url(flow.exerciseSettings('t1', 's1', 'set1'))).toBe('/training/exercisesettings/t1/s1?setId=set1');
  });

  it('is not embedded by default', () => {
    expect(flow.embedded).toBe(false);
  });

  it('can be replaced by a subclass, as the onboarding wizard does on its route', () => {
    @Injectable()
    class WizardFlow extends TrainingFlow {
      override readonly embedded = true;
      override programCreated(programId: string): UrlTree {
        return this.router.createUrlTree(['/wizard', programId, 'template']);
      }
    }
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: TrainingFlow, useClass: WizardFlow }],
    });
    const wizard = TestBed.inject(TrainingFlow);
    router = TestBed.inject(Router);

    expect(wizard.embedded).toBe(true);
    expect(url(wizard.programCreated('p1'))).toBe('/wizard/p1/template');
    // What the subclass does not override keeps the default route
    expect(url(wizard.sessionSets('t1', 's1'))).toBe('/training/sessionsets/t1/s1');
  });
});
