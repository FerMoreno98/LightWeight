import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { TrainingFlow } from '../shared/training-flow';
import { OnboardingFlow } from './onboarding-flow';

describe('OnboardingFlow', () => {
  let flow: TrainingFlow;
  let router: Router;
  const url = (tree: UrlTree) => router.serializeUrl(tree);
  /** Pretends the wizard is showing this page */
  const at = (path: string) => vi.spyOn(router, 'url', 'get').mockReturnValue(path);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: TrainingFlow, useClass: OnboardingFlow }],
    });
    flow = TestBed.inject(TrainingFlow);
    router = TestBed.inject(Router);
  });

  it('is embedded', () => {
    expect(flow.embedded).toBe(true);
  });

  it('keeps the forms inside the wizard, in the program of the current page', () => {
    at('/training/onboarding/p1/template');
    expect(url(flow.createProgram())).toBe('/training/onboarding/program');
    expect(url(flow.programCreated('p2'))).toBe('/training/onboarding/p2/template');
    expect(url(flow.templateSessions('t1'))).toBe('/training/onboarding/p1/sessions/t1');
    expect(url(flow.sessionSets('t1', 's1'))).toBe('/training/onboarding/p1/sets/t1/s1');
    expect(url(flow.exerciseSettings('t1', 's1'))).toBe('/training/onboarding/p1/exercise/t1/s1');
    expect(url(flow.exerciseSettings('t1', 's1', 'set1'))).toBe('/training/onboarding/p1/exercise/t1/s1?setId=set1');
  });

  it('resumes the wizard when there is no program in the current page', () => {
    at('/training/onboarding/program');
    expect(url(flow.templateSessions('t1'))).toBe('/training/onboarding');
    expect(url(flow.templatesList())).toBe('/training/onboarding');
  });
});
