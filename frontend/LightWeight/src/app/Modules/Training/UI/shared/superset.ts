/**
 * Supersets: sets sharing a superSetGroupId are performed alternating their exercises, round by round
 * (round r = the r-th set of each exercise). Shared by the template screen and the session screen
 */

/** Consecutive sets of the same exercise and superset, in the order they were planned */
export interface ExerciseRun<T> {
  exerciseId: string;
  superSetGroupId: string | null;
  sets: T[];
}

/** A unit of a session: an exercise on its own, or a superset with its exercises (A1, A2...) */
export type SuperSetUnit<T> =
  | { kind: 'single'; item: T }
  | { kind: 'superset'; groupId: string; label: string; items: T[] };

/** Groups consecutive sets of the same exercise and superset, keeping the planned order */
export function groupConsecutive<T extends { exerciseId: string; superSetGroupId: string | null }>(sets: T[]): ExerciseRun<T>[] {
  const runs: ExerciseRun<T>[] = [];
  for (const set of sets) {
    const last = runs.at(-1);
    if (last && last.exerciseId === set.exerciseId && last.superSetGroupId === set.superSetGroupId) {
      last.sets.push(set);
    } else {
      runs.push({ exerciseId: set.exerciseId, superSetGroupId: set.superSetGroupId, sets: [set] });
    }
  }
  return runs;
}

/**
 * Joins the items that share a superset into one unit, placed where its first item is.
 * A group with a single item is not a superset. Supersets are labelled A, B, C... in order of appearance
 */
export function buildUnits<T>(items: T[], groupOf: (item: T) => string | null): SuperSetUnit<T>[] {
  const membersByGroup = new Map<string, T[]>();
  for (const item of items) {
    const group = groupOf(item);
    if (group) membersByGroup.set(group, [...(membersByGroup.get(group) ?? []), item]);
  }

  const units: SuperSetUnit<T>[] = [];
  const placed = new Set<string>();
  let letter = 0;
  for (const item of items) {
    const group = groupOf(item);
    const members = group ? membersByGroup.get(group)! : [];
    if (!group || members.length < 2) {
      units.push({ kind: 'single', item });
    } else if (!placed.has(group)) {
      placed.add(group);
      units.push({ kind: 'superset', groupId: group, label: supersetLetter(letter++), items: members });
    }
  }
  return units;
}

/** A, B, ..., Z, AA, AB... */
export function supersetLetter(index: number): string {
  let label = '';
  let n = index;
  do {
    label = String.fromCharCode(65 + (n % 26)) + label;
    n = Math.floor(n / 26) - 1;
  } while (n >= 0);
  return label;
}
