import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { PerformedSet, PreviousExercise, Session, SessionWorkout, Set, SetValues } from '../../../data/training-api.service';
import { TrainingStore } from '../../../state/training.store';
import { SessionPlan } from './session-plan';

const planned = (id: string, exerciseId: string, exerciseName: string, min: number, max: number, rpe: number, superSetGroupId: string | null = null): Set => ({
  id, exerciseId, exerciseName, repetitionRangeMin: min, repetitionRangeMax: max, expectedRPE: rpe,
  technique: null, superSetGroupId, aimMuscleGroups: ['Quads'],
});

/** Store with the plan of a session in memory: registering, correcting and removing sets change the workout */
class FakeStore {
  offline = false;
  calls: { method: string; args: unknown[] }[] = [];
  private nextId = 1;

  error = signal<string | null>(null);
  exercises = signal([]);
  sessions = signal<Session[]>([{ id: 's1', name: 'Lower A', setsCount: 7, exercisesCount: 4 }]);
  sets = signal<Set[]>([
    planned('sq1', 'squat', 'Sentadilla', 4, 6, 8),
    planned('sq2', 'squat', 'Sentadilla', 10, 12, 9),
    planned('sq3', 'squat', 'Sentadilla', 10, 12, 9),
    // A superset is planned exercise by exercise; it is performed round by round
    planned('le1', 'ext', 'Leg extensions', 12, 15, 9, 'g1'),
    planned('le2', 'ext', 'Leg extensions', 12, 15, 9, 'g1'),
    planned('lc1', 'curl', 'Leg curl', 10, 12, 9, 'g1'),
    planned('lc2', 'curl', 'Leg curl', 10, 12, 9, 'g1'),
  ]);
  sessionWorkout = signal<SessionWorkout | null>({
    canStart: false,
    session: { id: 'ts1', startAt: '2026-11-18T17:30:00Z', finishedAt: null, duration: '00:00:00', motivationLevel: 7, sleepLevel: 7, domsLevel: 3, comments: null, sets: [] },
  });
  previousPerformance = signal<Record<string, PreviousExercise>>({
    squat: { exerciseId: 'squat', performedAt: '2026-11-11T17:30:00Z', sets: [
      { repetitions: 5, weight: 100, rpe: 8, isBodyWeight: false },
      { repetitions: 11, weight: 80, rpe: 9, isBodyWeight: false },
      { repetitions: 10, weight: 80, rpe: 9, isBodyWeight: false },
    ] },
  });

  private performed(): PerformedSet[] { return this.sessionWorkout()!.session!.sets; }
  private setPerformed(sets: PerformedSet[]) {
    const workout = this.sessionWorkout()!;
    this.sessionWorkout.set({ ...workout, session: { ...workout.session!, sets } });
  }

  async GetSetsFromASessionTemplate() { return true; }
  async GetSessionWorkout() { return true; }
  async GetSessionsFromATrainingTemplate() { return true; }
  async GetPreviousPerformance() { return true; }
  async GetAllExercises() { return true; }
  ClearError() { this.error.set(null); }

  async RegisterSet(_templateSessionId: string, _sessionId: string, templateSetId: string | null, superSetGroupId: string | null, values: SetValues) {
    this.calls.push({ method: 'RegisterSet', args: [templateSetId, superSetGroupId, values] });
    if (this.offline) return false;
    const name = this.sets().find(s => s.exerciseId === values.exerciseId)?.exerciseName ?? '';
    this.setPerformed([...this.performed(), { id: `done-${this.nextId++}`, exerciseName: name, templateSetId, superSetGroupId, ...values }]);
    return true;
  }

  async UpdateSet(_templateSessionId: string, _sessionId: string, setId: string, values: SetValues) {
    this.calls.push({ method: 'UpdateSet', args: [setId, values] });
    if (this.offline) return false;
    this.setPerformed(this.performed().map(s => s.id === setId ? { ...s, ...values } : s));
    return true;
  }

  async RemoveSet(_templateSessionId: string, _sessionId: string, setId: string) {
    this.calls.push({ method: 'RemoveSet', args: [setId] });
    if (this.offline) return false;
    this.setPerformed(this.performed().filter(s => s.id !== setId));
    return true;
  }
}

async function render() {
  localStorage.clear();
  const store = new FakeStore();
  await TestBed.configureTestingModule({
    imports: [SessionPlan],
    providers: [
      provideRouter([]),
      { provide: TrainingStore, useValue: store },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ templateid: 't1', sessionid: 's1' }) } } },
    ],
  }).compileComponents();
  const fixture = TestBed.createComponent(SessionPlan);
  await fixture.whenStable();
  const page = fixture.componentInstance;
  const squat = () => page.rowsOf(page.items()[0]);
  return { fixture, page, store, squat };
}

describe('SessionPlan', () => {
  it('shows every tranche of the plan and divides the rows where the target changes', async () => {
    const { page } = await render();
    const card = page.cards()[0];

    expect(card.kind === 'exercise' && card.heading.scheme).toBe('1 × 4–6 @ RPE 8 · 2 × 10–12 @ RPE 9');
    const dividers = card.entries.filter(e => e.kind === 'divider').map(e => e.kind === 'divider' && e.label);
    expect(dividers).toEqual(['4–6 reps · RPE 8', '10–12 reps · RPE 9']);
  });

  it('shows a superset round by round: A1, A2, then the next round', async () => {
    const { page } = await render();
    const superset = page.cards()[1];

    expect(superset.kind).toBe('superset');
    const lines = superset.entries.map(e => e.kind === 'divider' ? e.label : `${e.row.label}:${e.row.planned!.id}`);
    expect(lines).toEqual(['Ronda 1', 'A1:le1', 'A2:lc1', 'Ronda 2', 'A1:le2', 'A2:lc2']);
  });

  it('marks a set with the previous time and the planned RPE, and starts the rest', async () => {
    const { page, store, squat } = await render();

    await page.toggle(squat()[1]);

    expect(store.calls[0]).toEqual({ method: 'RegisterSet', args: ['sq2', null, { exerciseId: 'squat', weight: 80, repetitions: 11, rpe: 9, isBodyWeight: false }] });
    expect(page.viewOf(squat()[1]).done).toBe(true);
    expect(page.rest()?.label).toBe('Sentadilla · serie 2');
  });

  it('undoing a set deletes it but keeps its values in the row', async () => {
    const { page, store, squat } = await render();
    await page.toggle(squat()[0]);

    await page.toggle(squat()[0]);

    expect(store.calls.at(-1)?.method).toBe('RemoveSet');
    const view = page.viewOf(squat()[0]);
    expect(view.done).toBe(false);
    expect([view.weight, view.repetitions, view.hint.weight]).toEqual(['100', '5', false]);
  });

  it('without connection the set stays marked as unsaved, blocks the finish and is sent again later', async () => {
    const { page, store, squat } = await render();
    store.offline = true;

    await page.toggle(squat()[0]);

    expect(page.viewOf(squat()[0])).toMatchObject({ done: true, status: 'unsaved' });
    expect(page.hasPending()).toBe(true);

    store.offline = false;
    page.retryUnsaved();
    await TestBed.inject(TrainingStore).GetSessionWorkout('s1');
    await new Promise(resolve => setTimeout(resolve));

    expect(page.viewOf(squat()[0])).toMatchObject({ done: true, status: null });
    expect(page.hasPending()).toBe(false);
  });

  it('opens the set when there is no previous time to repeat', async () => {
    const { page, store } = await render();
    const extension = page.rowsOf(page.items()[1])[0];

    await page.toggle(extension);

    expect(store.calls).toEqual([]);
    expect(page.sheet()?.row.key).toBe(extension.key);
    expect(page.sheet()?.field).toBe('weight');
  });

  it('keeps what is typed in the sheet as a draft when it is closed', async () => {
    const { fixture, page, squat } = await render();
    page.openSheet(squat()[0], 'weight');

    page.closeSheet({ weight: 102.5, repetitions: 5, rpe: 8 });
    await fixture.whenStable();

    expect(page.viewOf(squat()[0])).toMatchObject({ done: false, weight: '102,5', hint: { weight: false, repetitions: false, rpe: false } });
    expect(JSON.parse(localStorage.getItem('lightweight.session-drafts.ts1')!).values['p-sq1']).toEqual({ weight: 102.5, repetitions: 5, rpe: 8 });
  });

  it('adds an extra set to a planned exercise and registers it outside the plan', async () => {
    const { page, store, squat } = await render();

    page.addSet('plan-0');
    const extra = squat()[3];
    expect(extra.planned).toBeNull();
    // Nothing to repeat from a fourth set the previous time: it opens the set instead of making up the reps
    await page.toggle(extra);
    expect(page.sheet()?.row.key).toBe(extra.key);
    await page.saveSheet({ weight: 70, repetitions: 12, rpe: 9 });

    expect(store.calls[0]).toEqual({ method: 'RegisterSet', args: [null, null, { exerciseId: 'squat', weight: 70, repetitions: 12, rpe: 9, isBodyWeight: false }] });
    expect(squat()).toHaveLength(4);
    expect(squat()[3].performed).not.toBeNull();
    expect(page.drafts().extraRows['plan-0']).toBe(0);
  });

  it('warns before finishing with planned sets not marked', async () => {
    const { page, squat } = await render();
    await page.toggle(squat()[0]);

    expect(page.uncheckedPlanned()).toBe(6);
    expect(page.finishMessage()).toContain('Quedan 6 series planificadas sin marcar');
  });

  it('once finished, compares every set with the previous time and counts the skipped ones', async () => {
    const { page, store, squat } = await render();
    await page.toggle(squat()[0]);                       // 100×5 @ 8, the same as last time
    page.openSheet(squat()[1], 'weight');
    await page.saveSheet({ weight: 82.5, repetitions: 11, rpe: 9 });
    const workout = store.sessionWorkout()!;
    store.sessionWorkout.set({ ...workout, session: { ...workout.session!, finishedAt: '2026-11-18T18:40:00Z', duration: '01:10:00' } });

    const rows = squat();
    expect(page.viewOf(rows[0]).comparison).toEqual({ today: '100×5', rpe: '8', change: '=', trend: 'same' });
    expect(page.viewOf(rows[1]).comparison).toEqual({ today: '82,5×11', rpe: '9', change: '+2,5 kg', trend: 'up' });
    expect(page.viewOf(rows[2]).comparison).toEqual({ today: '—', rpe: null, change: 'Saltada', trend: 'skipped' });
    expect(page.finishedSummary()).toEqual({ done: 2, planned: 7, better: 1, compared: 2 });
  });
});
