import { AdvancedTechnique, PerformedSet, Set } from '../../data/training-api.service';
import { TECHNIQUE_LABELS } from '../shared/cycle-format';

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

// ---------------------------------------------------------------- Rows of the table

/** What a set is planned to be */
export interface RowTarget {
  min: number;
  max: number;
  rpe: number;
  technique: AdvancedTechnique | null;
}

/** Values of a set, complete: what is sent to the API */
export interface RowValues {
  weight: number;
  repetitions: number;
  rpe: number;
  isBodyWeight: boolean;
}

/** Values typed in a row that has not been marked done yet (any of them may be missing) */
export type DraftValues = Partial<RowValues>;

/** A row of the table of an exercise: a planned set or an extra one, done today or not */
export interface SetRow {
  /** Stable id of the row: "p-{templateSetId}" or "x-{ownerKey}-{n}" */
  key: string;
  /** Item (exercise of the session) the row belongs to */
  ownerKey: string;
  /** 1-based position of the set in its exercise */
  position: number;
  /** "A1" when the row is shown in the rounds of a superset */
  label: string | null;
  /** Exercise the set is (or will be) performed with */
  exerciseId: string;
  exerciseName: string;
  planned: Set | null;
  performed: PerformedSet | null;
  /** Same position the previous time the exercise was done */
  previous: RowValues | null;
  target: RowTarget | null;
  /** Values of the row before it in the same exercise today, as a fallback for the weight */
  prior: RowValues | null;
}

export function targetOf(set: Set): RowTarget {
  return { min: set.repetitionRangeMin, max: set.repetitionRangeMax, rpe: set.expectedRPE, technique: set.technique };
}

const sameTarget = (a: RowTarget, b: RowTarget) =>
  a.min === b.min && a.max === b.max && a.rpe === b.rpe && a.technique === b.technique;

/** Planned sets grouped by consecutive equal targets: [{count 1, 4–6 @ 8}, {count 2, 10–12 @ 9}] */
export function targetTranches(targets: RowTarget[]): { count: number; target: RowTarget }[] {
  const tranches: { count: number; target: RowTarget }[] = [];
  for (const target of targets) {
    const last = tranches.at(-1);
    if (last && sameTarget(last.target, target)) last.count++;
    else tranches.push({ count: 1, target });
  }
  return tranches;
}

/** "4–6 reps · RPE 8", plus the technique when there is one */
export function targetLabel(target: RowTarget): string {
  const technique = target.technique ? ` · ${TECHNIQUE_LABELS[target.technique]}` : '';
  return `${target.min}–${target.max} reps · RPE ${formatNumber(target.rpe)}${technique}`;
}

/** "3 × 8–12 @ RPE 8", or "1 × 4–6 @ RPE 8 · 2 × 10–12 @ RPE 9" when the targets change */
export function schemeOf(targets: RowTarget[]): string {
  return targetTranches(targets)
    .map(({ count, target }) => {
      const technique = target.technique ? ` (${TECHNIQUE_LABELS[target.technique]})` : '';
      return `${count} × ${target.min}–${target.max} @ RPE ${formatNumber(target.rpe)}${technique}`;
    })
    .join(' · ');
}

/**
 * What a row saves when it is marked done without opening it: what was typed, and for the rest the previous time
 * (weight and reps), the planned RPE, and the weight of the set before today when there is no previous time
 */
export function defaultValues(row: SetRow, draft: DraftValues | undefined): DraftValues {
  return {
    weight: draft?.weight ?? row.previous?.weight ?? row.prior?.weight,
    repetitions: draft?.repetitions ?? row.previous?.repetitions,
    rpe: draft?.rpe ?? row.target?.rpe ?? row.previous?.rpe,
    isBodyWeight: draft?.isBodyWeight ?? row.previous?.isBodyWeight ?? row.prior?.isBodyWeight ?? false,
  };
}

/** The values ready to be sent, or null if something is missing or out of range (RPE 1–10, whole reps) */
export function completeValues(values: DraftValues): RowValues | null {
  const { weight, repetitions, rpe } = values;
  if (weight === undefined || weight === null || !(weight >= 0)) return null;
  if (repetitions === undefined || repetitions === null || !(repetitions >= 0) || !Number.isInteger(repetitions)) return null;
  if (rpe === undefined || rpe === null || !(rpe >= 1 && rpe <= 10)) return null;
  return { weight, repetitions, rpe, isBodyWeight: values.isBodyWeight ?? false };
}

export function valuesOf(set: { weight: number; repetitions: number; rpe: number; isBodyWeight: boolean }): RowValues {
  return { weight: set.weight, repetitions: set.repetitions, rpe: set.rpe, isBodyWeight: set.isBodyWeight };
}

// ---------------------------------------------------------------- Comparing with the previous time

/** Above this (reps + reps in reserve) there is no estimation, as in the dashboard */
const ESTIMATED_UP_TO = 30;

/**
 * Estimated 1RM with Epley's formula counting the reps left in reserve (RIR = 10 - RPE): weight × (1 + (reps + RIR) / 30).
 * The same rule as the dashboard (OneRepMax.Estimate): no estimation for body weight sets, sets with a technique
 * (they perform worse by design), without load or reps, or too long
 */
export function estimateOneRepMax(set: RowValues, technique: AdvancedTechnique | null): number | null {
  if (set.isBodyWeight || technique || set.weight <= 0 || set.repetitions <= 0) return null;
  const toFailure = set.repetitions + (10 - set.rpe);
  if (toFailure > ESTIMATED_UP_TO) return null;
  return Math.round(set.weight * (1 + toFailure / 30) * 10) / 10;
}

export type Trend = 'up' | 'down' | 'same';

/** What changed in a set against the same set the previous time, and whether it went better or worse overall */
export interface SetComparison {
  /** "+5 kg −2 rep", "+1 rep", "RPE 8 → 9", "=" */
  change: string;
  /** Null when there is nothing to compare with */
  trend: Trend | null;
}

const signed = (value: number, unit: string) => `${value > 0 ? '+' : '−'}${formatNumber(Math.abs(value))}${unit}`;

/**
 * Compares a set with the same one the previous time. The overall trend uses the estimated 1RM, so 125×10 after
 * 120×12 can still be better; when there is no estimation (body weight, techniques) more load wins, then more reps,
 * then a lower RPE (the same work felt easier)
 */
export function compareSet(today: RowValues, previous: RowValues | null, technique: AdvancedTechnique | null): SetComparison {
  if (!previous) return { change: 'Primera vez', trend: null };
  const weight = today.weight - previous.weight;
  const reps = today.repetitions - previous.repetitions;
  const rpe = today.rpe - previous.rpe;
  const parts = [weight !== 0 ? signed(weight, ' kg') : '', reps !== 0 ? signed(reps, ' rep') : ''].filter(Boolean);
  const change = parts.length ? parts.join(' ') : rpe !== 0 ? `RPE ${formatNumber(previous.rpe)} → ${formatNumber(today.rpe)}` : '=';

  const now = estimateOneRepMax(today, technique);
  const before = estimateOneRepMax(previous, technique);
  let difference: number;
  if (now !== null && before !== null && today.isBodyWeight === previous.isBodyWeight) {
    difference = now - before;
  } else {
    difference = weight !== 0 ? weight : reps !== 0 ? reps : -rpe;
  }
  const trend: Trend = Math.abs(difference) < 0.05 ? 'same' : difference > 0 ? 'up' : 'down';
  return { change, trend };
}

// ---------------------------------------------------------------- Formats

const NUMBER_FORMAT = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 2 });

export function formatNumber(value: number): string {
  return NUMBER_FORMAT.format(value);
}

/** Load of a set in a table cell: "17,5", "PC" (body weight) or "PC+10" (body weight with extra load) */
export function formatLoad(weight: number, isBodyWeight: boolean): string {
  if (!isBodyWeight) return NUMBER_FORMAT.format(weight);
  return weight > 0 ? `PC+${NUMBER_FORMAT.format(weight)}` : 'PC';
}

/** "17,5×11" or "PC×9", for the Anterior column */
export function formatShortSet(set: { weight: number; repetitions: number; isBodyWeight: boolean }): string {
  return `${formatLoad(set.weight, set.isBodyWeight)}×${set.repetitions}`;
}

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
