import { SetRow, compareSet, completeValues, defaultValues, estimateOneRepMax, formatLoad, formatShortSet, schemeOf, targetTranches } from './session-workout.model';

const t = (min: number, max: number, rpe: number) => ({ min, max, rpe, technique: null });

const row = (overrides: Partial<SetRow> = {}): SetRow => ({
  key: 'p-1', ownerKey: 'plan-0', position: 1, label: null, exerciseId: 'e1', exerciseName: 'Sentadilla',
  planned: null, performed: null, previous: null, target: t(8, 12, 8), prior: null, ...overrides,
});

describe('schemeOf', () => {
  it('summarizes equal sets in one tranche', () => {
    expect(schemeOf([t(8, 12, 8), t(8, 12, 8), t(8, 12, 8)])).toBe('3 × 8–12 @ RPE 8');
  });

  it('keeps every tranche when the target changes, not only the first set', () => {
    expect(schemeOf([t(4, 6, 8), t(10, 12, 9), t(10, 12, 9)])).toBe('1 × 4–6 @ RPE 8 · 2 × 10–12 @ RPE 9');
    expect(targetTranches([t(4, 6, 8), t(10, 12, 9), t(10, 12, 9)]).map(x => x.count)).toEqual([1, 2]);
  });

  it('names the technique of a tranche', () => {
    expect(schemeOf([{ min: 12, max: 15, rpe: 9.5, technique: 'MyoRep' }])).toBe('1 × 12–15 @ RPE 9,5 (Myo-reps)');
  });
});

describe('defaultValues', () => {
  it('takes what was typed first, then the previous time and the planned RPE', () => {
    const previous = { weight: 80, repetitions: 11, rpe: 9, isBodyWeight: false };

    expect(defaultValues(row({ previous }), undefined)).toEqual({ weight: 80, repetitions: 11, rpe: 8, isBodyWeight: false });
    expect(defaultValues(row({ previous }), { weight: 82.5 })).toEqual({ weight: 82.5, repetitions: 11, rpe: 8, isBodyWeight: false });
  });

  it('uses the weight of the set before today when there is no previous time, but never makes up the reps', () => {
    const prior = { weight: 60, repetitions: 10, rpe: 8, isBodyWeight: false };

    expect(defaultValues(row({ prior }), undefined)).toEqual({ weight: 60, repetitions: undefined, rpe: 8, isBodyWeight: false });
  });
});

describe('completeValues', () => {
  it('accepts whole reps, any weight from zero and an RPE from 1 to 10', () => {
    expect(completeValues({ weight: 0, repetitions: 9, rpe: 8, isBodyWeight: true })).toEqual({ weight: 0, repetitions: 9, rpe: 8, isBodyWeight: true });
  });

  it('rejects what the API would reject', () => {
    expect(completeValues({ weight: 50, repetitions: 8.5, rpe: 8 })).toBeNull();
    expect(completeValues({ weight: 50, repetitions: 8, rpe: 11 })).toBeNull();
    expect(completeValues({ weight: -1, repetitions: 8, rpe: 8 })).toBeNull();
    expect(completeValues({ repetitions: 8, rpe: 8 })).toBeNull();
  });
});

describe('formats', () => {
  it('writes body weight as PC, with the extra load when there is one', () => {
    expect(formatLoad(17.5, false)).toBe('17,5');
    expect(formatLoad(0, true)).toBe('PC');
    expect(formatLoad(10, true)).toBe('PC+10');
    expect(formatShortSet({ weight: 0, repetitions: 9, isBodyWeight: true })).toBe('PC×9');
  });
});

describe('estimateOneRepMax', () => {
  const v = (weight: number, repetitions: number, rpe: number, isBodyWeight = false) => ({ weight, repetitions, rpe, isBodyWeight });

  it('uses the same formula as the dashboard: 100 kg × 10 @ RPE 8 is 140 kg', () => {
    expect(estimateOneRepMax(v(100, 10, 8), null)).toBe(140);
  });

  it('does not estimate body weight sets, techniques or too long sets', () => {
    expect(estimateOneRepMax(v(0, 10, 8, true), null)).toBeNull();
    expect(estimateOneRepMax(v(60, 12, 9), 'DropSet')).toBeNull();
    expect(estimateOneRepMax(v(20, 30, 8), null)).toBeNull();
  });
});

describe('compareSet', () => {
  const v = (weight: number, repetitions: number, rpe: number, isBodyWeight = false) => ({ weight, repetitions, rpe, isBodyWeight });

  it('names what changed and judges the whole set by its estimated 1RM', () => {
    // 125 × 10 @ 8 → 175 kg against 120 × 12 @ 8 → 176 kg: more load, but slightly worse; 125 × 11 → 179,2 kg is better
    expect(compareSet(v(125, 10, 8), v(120, 12, 8), null)).toEqual({ change: '+5 kg −2 rep', trend: 'down' });
    expect(compareSet(v(125, 11, 8), v(120, 12, 8), null)).toEqual({ change: '+5 kg −1 rep', trend: 'up' });
  });

  it('counts the same set with a lower RPE as better', () => {
    expect(compareSet(v(80, 10, 8), v(80, 10, 9), null)).toEqual({ change: 'RPE 9 → 8', trend: 'up' });
    expect(compareSet(v(80, 10, 8), v(80, 10, 8), null)).toEqual({ change: '=', trend: 'same' });
  });

  it('without an estimation, more load wins, then more reps', () => {
    expect(compareSet(v(0, 10, 8, true), v(0, 9, 8, true), null)).toEqual({ change: '+1 rep', trend: 'up' });
    expect(compareSet(v(5, 8, 8, true), v(0, 10, 8, true), null)).toEqual({ change: '+5 kg −2 rep', trend: 'up' });
  });

  it('has nothing to compare with the first time', () => {
    expect(compareSet(v(80, 10, 8), null, null)).toEqual({ change: 'Primera vez', trend: null });
  });
});
