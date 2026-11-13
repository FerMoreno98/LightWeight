import { SetupTemplate, TrainingSetupStatus } from '../../data/training-api.service';
import { nextStep, parseOnboardingUrl, resumeUrl, stepStates, stepUrl } from './onboarding-steps';

const template = (id: string, sessions: number, sessionsWithSets: number, volumeLandmark: SetupTemplate['volumeLandmark'] = 'MEV'): SetupTemplate =>
  ({ id, name: id, volumeLandmark, sessions, sessionsWithSets });

const status = (overrides: Partial<TrainingSetupStatus> = {}): TrainingSetupStatus => ({
  step: 'CreateProgram',
  canLaunch: false,
  isFirstTime: true,
  programId: null,
  programName: null,
  templates: [],
  ...overrides,
});

const withProgram = (templates: SetupTemplate[], step: TrainingSetupStatus['step']) =>
  status({ step, programId: 'p1', programName: 'PPL', templates, canLaunch: step === 'AddVolumeLevels' || step === 'LaunchBlock' });

describe('parseOnboardingUrl', () => {
  it('reads the step and the program of every wizard page', () => {
    expect(parseOnboardingUrl('/training/onboarding/program')).toEqual({ step: 'program', programId: null });
    expect(parseOnboardingUrl('/training/onboarding/p1/template')).toEqual({ step: 'template', programId: 'p1' });
    expect(parseOnboardingUrl('/training/onboarding/p1/sessions/t1')).toEqual({ step: 'sessions', programId: 'p1' });
    expect(parseOnboardingUrl('/training/onboarding/p1/sets/t1/s1')).toEqual({ step: 'sessions', programId: 'p1' });
    expect(parseOnboardingUrl('/training/onboarding/p1/exercise/t1/s1?setId=x')).toEqual({ step: 'sessions', programId: 'p1' });
    expect(parseOnboardingUrl('/training/onboarding/p1/levels')).toEqual({ step: 'levels', programId: 'p1' });
    expect(parseOnboardingUrl('/training/onboarding/p1/launch')).toEqual({ step: 'launch', programId: 'p1' });
  });

  it('is no step outside the wizard', () => {
    expect(parseOnboardingUrl('/training/sessiontemplate/t1')).toEqual({ step: null, programId: null });
    expect(parseOnboardingUrl('/training/onboarding')).toEqual({ step: null, programId: null });
  });
});

describe('resumeUrl', () => {
  it('goes to the page of the step the backend says is next', () => {
    expect(resumeUrl(status())).toBe('/training/onboarding/program');
    expect(resumeUrl(withProgram([], 'CreateTemplate'))).toBe('/training/onboarding/p1/template');
    expect(resumeUrl(withProgram([template('t1', 1, 0)], 'FillSessions'))).toBe('/training/onboarding/p1/sessions/t1');
    expect(resumeUrl(withProgram([template('t1', 1, 1)], 'AddVolumeLevels'))).toBe('/training/onboarding/p1/levels');
    expect(resumeUrl(withProgram([template('t1', 1, 1), template('t2', 1, 1, 'MAV')], 'LaunchBlock'))).toBe('/training/onboarding/p1/launch');
  });

  it('sends to the home once the user is training', () => {
    expect(resumeUrl(withProgram([template('t1', 1, 1)], 'Done'))).toBe('/home');
  });
});

describe('stepStates', () => {
  it('locks the steps whose previous step is missing', () => {
    expect(stepStates(status(), 'program')).toEqual({
      program: 'current', template: 'locked', sessions: 'locked', levels: 'locked', launch: 'locked',
    });
  });

  it('marks what is saved as done and lets launch be opened without levels (they are optional)', () => {
    const states = stepStates(withProgram([template('t1', 2, 2)], 'AddVolumeLevels'), 'levels');
    expect(states).toEqual({ program: 'done', template: 'done', sessions: 'done', levels: 'current', launch: 'available' });
  });

  it('keeps the sessions step pending while a session has no sets', () => {
    const states = stepStates(withProgram([template('t1', 2, 1)], 'FillSessions'), 'sessions');
    expect(states.sessions).toBe('current');
    expect(states.levels).toBe('locked');
    expect(states.launch).toBe('locked');
  });
});

describe('stepUrl', () => {
  it('does not reopen the program step once the program exists (there is no page to edit it)', () => {
    expect(stepUrl('program', withProgram([], 'CreateTemplate'))).toBeNull();
  });

  it('has no url for a locked step', () => {
    expect(stepUrl('launch', withProgram([template('t1', 1, 0)], 'FillSessions'))).toBeNull();
  });
});

describe('nextStep', () => {
  it('offers the next step once the current one is done', () => {
    expect(nextStep(withProgram([template('t1', 1, 1)], 'AddVolumeLevels'), 'sessions')).toBe('levels');
  });

  it('offers nothing while the current step is not done', () => {
    expect(nextStep(withProgram([template('t1', 1, 0)], 'FillSessions'), 'sessions')).toBeNull();
  });
});
