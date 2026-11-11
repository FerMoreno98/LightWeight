import { buildUnits, groupConsecutive, orderedSetIds } from './superset';

interface TestSet {
  id: string;
  exerciseId: string;
  superSetGroupId: string | null;
}

const set = (id: string, exerciseId: string, superSetGroupId: string | null = null): TestSet => ({ id, exerciseId, superSetGroupId });

/** Same grouping as the template screen: consecutive sets of an exercise, then supersets together */
const unitsOf = (sets: TestSet[]) => buildUnits(groupConsecutive(sets), g => g.superSetGroupId);

describe('orderedSetIds', () => {
  it('flattens single exercises and supersets in the order of the units', () => {
    const units = unitsOf([
      set('p1', 'press', 'A'), set('p2', 'press', 'A'),
      set('c1', 'curl'),
      set('d1', 'pullup', 'A'), set('d2', 'pullup', 'A'),
    ]);

    expect(orderedSetIds(units)).toEqual(['p1', 'p2', 'd1', 'd2', 'c1']);
  });

  it('follows the units after moving one (the sets of a unit move together)', () => {
    const units = unitsOf([set('p1', 'press'), set('p2', 'press'), set('c1', 'curl'), set('s1', 'squat')]);
    const [press, curl, squat] = units;

    expect(orderedSetIds([squat, press, curl])).toEqual(['s1', 'p1', 'p2', 'c1']);
  });

  it('keeps every set exactly once', () => {
    const sets = [set('a', 'x', 'G'), set('b', 'y'), set('c', 'z', 'G'), set('d', 'y')];

    const ids = orderedSetIds(unitsOf(sets));

    expect([...ids].sort()).toEqual(['a', 'b', 'c', 'd']);
  });
});
