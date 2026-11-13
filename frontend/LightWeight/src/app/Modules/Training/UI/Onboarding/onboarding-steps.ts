import { SetupTemplate, TrainingSetupStatus, TrainingSetupStep, VolumeLandmark } from '../../data/training-api.service';

/**
 * Pure logic of the onboarding wizard: which step a URL is, what the minimap shows for each step
 * and where to resume. Everything is worked out from the backend setup status, nothing else is stored
 */

export const ONBOARDING_BASE = '/training/onboarding';

/** Steps of the wizard, in order. "sessions" also covers the sets and exercise pages of a session */
export type OnboardingStepKey = 'program' | 'template' | 'sessions' | 'levels' | 'launch';
export const ONBOARDING_STEPS: OnboardingStepKey[] = ['program', 'template', 'sessions', 'levels', 'launch'];

/** done: already saved · current: the page being shown · available: can be opened · locked: a previous step is missing */
export type OnboardingStepState = 'done' | 'current' | 'available' | 'locked';

export interface OnboardingLocation {
  step: OnboardingStepKey | null;
  programId: string | null;
}

/** Sub-pages of each step (the segment after the program id) */
const STEP_BY_SEGMENT: Record<string, OnboardingStepKey> = {
  template: 'template',
  sessions: 'sessions',
  sets: 'sessions',
  exercise: 'sessions',
  levels: 'levels',
  launch: 'launch',
};

/** Reads the step and the program from a wizard URL: /training/onboarding/program or /training/onboarding/:programId/<page>/... */
export function parseOnboardingUrl(url: string): OnboardingLocation {
  const path = url.split(/[?#]/)[0];
  if (!path.startsWith(ONBOARDING_BASE)) return { step: null, programId: null };
  const segments = path.slice(ONBOARDING_BASE.length).split('/').filter(Boolean);
  if (segments[0] === 'program') return { step: 'program', programId: null };
  if (segments.length < 2) return { step: null, programId: segments[0] ?? null };
  return { step: STEP_BY_SEGMENT[segments[1]] ?? null, programId: segments[0] };
}

/** The base template: the first one. The others are copies of it at other volume levels */
export function baseTemplate(status: TrainingSetupStatus): SetupTemplate | null {
  return status.templates[0] ?? null;
}

/** The base template has sessions and every one of them has sets */
export function isBaseFilled(status: TrainingSetupStatus): boolean {
  const base = baseTemplate(status);
  return !!base && base.sessions > 0 && base.sessionsWithSets === base.sessions;
}

function isStepDone(step: OnboardingStepKey, status: TrainingSetupStatus): boolean {
  switch (step) {
    case 'program': return status.programId !== null;
    case 'template': return status.templates.length > 0;
    case 'sessions': return isBaseFilled(status);
    case 'levels': return status.templates.length > 1;
    case 'launch': return status.step === 'Done';
  }
}

/** Whether the steps before this one allow opening it. Levels is optional, so launch only needs the sessions */
function isStepReachable(step: OnboardingStepKey, status: TrainingSetupStatus): boolean {
  switch (step) {
    case 'program': return true;
    case 'template': return isStepDone('program', status);
    case 'sessions': return isStepDone('template', status);
    case 'levels':
    case 'launch': return isStepDone('sessions', status);
  }
}

/** State of every step for the minimap */
export function stepStates(status: TrainingSetupStatus, current: OnboardingStepKey | null): Record<OnboardingStepKey, OnboardingStepState> {
  const states = {} as Record<OnboardingStepKey, OnboardingStepState>;
  for (const step of ONBOARDING_STEPS) {
    states[step] = step === current ? 'current'
      : isStepDone(step, status) ? 'done'
      : isStepReachable(step, status) ? 'available'
      : 'locked';
  }
  return states;
}

/** URL of a step, or null when it cannot be opened (a previous step is missing, or the program already exists) */
export function stepUrl(step: OnboardingStepKey, status: TrainingSetupStatus): string | null {
  if (!isStepReachable(step, status)) return null;
  const programId = status.programId;
  switch (step) {
    // There is no page to edit a program: once created, its step is not opened again
    case 'program': return programId ? null : `${ONBOARDING_BASE}/program`;
    case 'template': return `${ONBOARDING_BASE}/${programId}/template`;
    case 'sessions': {
      const base = baseTemplate(status);
      return base ? `${ONBOARDING_BASE}/${programId}/sessions/${base.id}` : null;
    }
    case 'levels': return `${ONBOARDING_BASE}/${programId}/levels`;
    case 'launch': return `${ONBOARDING_BASE}/${programId}/launch`;
  }
}

const STEP_BY_SETUP: Record<TrainingSetupStep, OnboardingStepKey | null> = {
  CreateProgram: 'program',
  CreateTemplate: 'template',
  FillSessions: 'sessions',
  AddVolumeLevels: 'levels',
  LaunchBlock: 'launch',
  Done: null,
};

/** Where to resume the wizard. Once the user is training there is nothing left: back to the home */
export function resumeUrl(status: TrainingSetupStatus): string {
  const step = STEP_BY_SETUP[status.step];
  return (step && stepUrl(step, status)) ?? '/home';
}

/** Next step to offer once the current one is done (levels is optional, so it is offered but launch is reachable) */
export function nextStep(status: TrainingSetupStatus, current: OnboardingStepKey | null): OnboardingStepKey | null {
  if (!current || !isStepDone(current, status)) return null;
  const next = ONBOARDING_STEPS[ONBOARDING_STEPS.indexOf(current) + 1];
  return next && isStepReachable(next, status) ? next : null;
}

export const STEP_LABELS: Record<OnboardingStepKey, string> = {
  program: 'Programa',
  template: 'Plantilla base',
  sessions: 'Sesiones y series',
  levels: 'Niveles de volumen',
  launch: 'Lanzar el bloque',
};

/** Default names of the template created for each landmark from the base one */
export const LEVEL_DEFAULT_NAMES: Record<VolumeLandmark, string> = {
  MV: 'Descarga',
  MEV: 'Base',
  MAV: 'Acumulación',
  MRV: 'Pico',
};

/** From less to more volume, the order the levels are offered in */
export const LANDMARK_ORDER: VolumeLandmark[] = ['MV', 'MEV', 'MAV', 'MRV'];
