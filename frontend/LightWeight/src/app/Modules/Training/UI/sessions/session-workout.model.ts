import { PerformedSet, PreviousExercise, Set } from '../../data/training-api.service';

/** Consecutive planned sets of the same exercise and superset, in the order they were planned */
export interface PlannedBlock {
  index: number;
  exerciseId: string;
  exerciseName: string;
  /** Superset of the plan (null when it is planned alone) */
  plannedGroupId: string | null;
  sets: Set[];
}

/** Extra sets (not planned) of one exercise */
export interface ExtraGroup {
  exerciseId: string;
  exerciseName: string;
  sets: PerformedSet[];
}

/** An exercise of the session: planned (a block) or extra. The key identifies it in supersets */
export type WorkoutItem =
  | { key: string; kind: 'plan'; block: PlannedBlock }
  | { key: string; kind: 'extra'; group: ExtraGroup };

/** Superset of the exercise in focus, for the round header and the A1/A2 pills */
export interface FocusSuperSet {
  label: string;
  round: number;
  rounds: number;
  members: { label: string; name: string; done: boolean; current: boolean }[];
}

/** Which slot the focus mode shows: a planned set of a block, or a set of an extra exercise */
export type FocusTarget =
  | { kind: 'plan'; block: number; set: number }
  | { kind: 'extra'; exerciseId: string; exerciseName: string; set: number };

/** One set position of the exercise in focus: what was planned (if any) and what was done today (if any) */
export interface FocusSlot {
  planned: Set | null;
  performed: PerformedSet | null;
}

/** Everything the focus mode needs to render the exercise in focus */
export interface FocusView {
  /** Changes when the slot in focus changes, so the form is filled again */
  key: string;
  kind: 'plan' | 'extra';
  exerciseId: string;
  exerciseName: string;
  /** Planned exercise when another one is being performed instead */
  replacedName: string | null;
  /** Whether the exercise can be replaced (or the replacement undone) for the pending sets */
  canReplace: boolean;
  /** Whether a replacement chosen in this screen can be undone */
  canRestore: boolean;
  setIndex: number;
  slots: FocusSlot[];
  previous: PreviousExercise | null;
  readOnly: boolean;
  hasPreviousExercise: boolean;
  hasNextExercise: boolean;
  superSet: FocusSuperSet | null;
  /** Label of the exercise the focus moves to after recording this set ("A2"), only in supersets */
  nextLabel: string | null;
}

/** Values typed in the "Hoy" card */
export interface SetForm {
  weight: number | null;
  repetitions: number | null;
  rpe: number | null;
  isBodyWeight: boolean;
}

const NUMBER_FORMAT = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 2 });

/** "80 kg × 8 @ 7,5", or "Peso corporal × 12 @ 9" */
export function formatPerformedSet(set: { weight: number; repetitions: number; rpe: number; isBodyWeight: boolean }): string {
  const load = set.isBodyWeight
    ? set.weight > 0 ? `PC + ${NUMBER_FORMAT.format(set.weight)} kg` : 'Peso corporal'
    : `${NUMBER_FORMAT.format(set.weight)} kg`;
  return `${load} × ${set.repetitions} @ ${NUMBER_FORMAT.format(set.rpe)}`;
}

/** Parses a .NET TimeSpan ("01:12:33.1234567") into "1 h 12 min" */
export function formatDuration(duration: string): string {
  const [hours, minutes] = duration.split(':').map(part => parseInt(part, 10) || 0);
  if (hours === 0) return `${minutes} min`;
  return minutes === 0 ? `${hours} h` : `${hours} h ${minutes} min`;
}

const TIME_FORMAT = new Intl.DateTimeFormat('es-ES', { hour: '2-digit', minute: '2-digit' });
const DAY_FORMAT = new Intl.DateTimeFormat('es-ES', { weekday: 'short', day: 'numeric', month: 'short' });

export function formatTime(isoDate: string): string {
  return TIME_FORMAT.format(new Date(isoDate));
}

export function formatDay(isoDate: string): string {
  return DAY_FORMAT.format(new Date(isoDate));
}
