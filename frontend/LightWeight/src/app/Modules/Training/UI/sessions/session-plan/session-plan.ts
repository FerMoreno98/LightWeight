import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NgTemplateOutlet } from '@angular/common';
import { TrainingStore } from '../../../state/training.store';
import { Exercise, PerformedSet } from '../../../data/training-api.service';
import { SuperSetUnit, buildUnits, groupConsecutive } from '../../shared/superset';
import { ConfirmDialog } from '../../../../../Shared/confirm-dialog/confirm-dialog';
import { StartSessionDialog, StartSessionForm } from '../start-session-dialog/start-session-dialog';
import { ExercisePicker } from '../exercise-picker/exercise-picker';
import { RowView, SetField, SetTable, TableEntry } from '../set-table/set-table';
import { SetSheet } from '../set-sheet/set-sheet';
import { RestStart, RestTimer } from '../rest-timer/rest-timer';
import {
  DraftValues, ExtraGroup, PlannedBlock, RowValues, SetRow, WorkoutItem,
  completeValues, defaultValues, formatDuration, formatLoad, formatNumber, formatShortSet, formatTime,
  schemeOf, targetLabel, targetOf, targetTranches, valuesOf,
} from '../session-workout.model';

/** Exercise chosen in this screen to replace the planned one of a block */
interface Replacement {
  exerciseId: string;
  exerciseName: string;
}

/** Superset of each exercise (by item key) decided in this screen; null = performed alone */
type SuperSetOverrides = Record<string, string | null>;

/** What is typed but not saved yet, kept in the browser per session */
interface StoredDrafts {
  values: Record<string, DraftValues>;
  /** Empty extra rows of each exercise, by item key */
  extraRows: Record<string, number>;
  /** Extra exercises added in this screen that have no set yet */
  added: Replacement[];
}

/** A change of a row on its way to the server. Unsaved ones are retried */
type PendingChange =
  | { op: 'register' | 'update'; row: SetRow; values: RowValues; status: 'saving' | 'unsaved'; attempts: number }
  | { op: 'remove'; status: 'saving' };

/** How an exercise is named on its card or in the legend of a superset */
interface ExerciseHeading {
  key: string;
  label: string | null;
  name: string;
  /** Planned exercise when another one is performed instead */
  replacedFrom: string | null;
  scheme: string;
  isPlan: boolean;
}

export type SessionCard =
  | { kind: 'exercise'; key: string; number: number | null; heading: ExerciseHeading; entries: TableEntry[]; complete: boolean; item: WorkoutItem }
  | { kind: 'superset'; key: string; label: string; members: ExerciseHeading[]; entries: TableEntry[]; progress: string; unit: SuperSetUnit<WorkoutItem> & { kind: 'superset' } };

const OVERRIDES_STORAGE_KEY = 'lightweight.session-supersets.';
const DRAFTS_STORAGE_KEY = 'lightweight.session-drafts.';
const EMPTY_DRAFTS: StoredDrafts = { values: {}, extraRows: {}, added: [] };

/**
 * A session of the week as a table per exercise: one row per set with the previous time, kg, reps, RPE and ✓.
 * Marking a set saves it at once in the background (it shows as saving, and as "sin guardar" while there is no
 * connection, retrying on its own). What is typed in unmarked rows stays as a draft in the browser.
 * Supersets show their sets round by round. Exercises can be replaced, and sets and exercises added
 */
@Component({
  selector: 'app-session-plan',
  standalone: true,
  imports: [RouterLink, NgTemplateOutlet, ConfirmDialog, StartSessionDialog, ExercisePicker, SetTable, SetSheet, RestTimer],
  templateUrl: './session-plan.html',
  styleUrl: './session-plan.css',
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(window:online)': 'retryUnsaved()',
  },
})
export class SessionPlan {
  private store = inject(TrainingStore);
  private route = inject(ActivatedRoute);

  error = this.store.error;
  exercises = this.store.exercises;
  isLoading = signal(true);
  private templateSessionId = '';

  templateSession = computed(() => this.store.sessions().find(s => s.id === this.templateSessionId) ?? null);
  workout = this.store.sessionWorkout;
  session = computed(() => this.workout()?.session ?? null);
  isFinished = computed(() => !!this.session()?.finishedAt);
  /** Sets can only be recorded while the session is in progress */
  canRecord = computed(() => !!this.session() && !this.isFinished());

  // ---------------------------------------------------------------- Plan and performed sets

  blocks = computed<PlannedBlock[]>(() =>
    groupConsecutive(this.store.sets()).map((run, index) => ({
      index,
      exerciseId: run.exerciseId,
      exerciseName: run.sets[0].exerciseName || 'Ejercicio eliminado',
      plannedGroupId: run.superSetGroupId,
      sets: run.sets,
    })));

  private performed = computed(() => this.session()?.sets ?? []);
  private performedByPlannedSet = computed(() =>
    new Map(this.performed().filter(s => s.templateSetId).map(s => [s.templateSetId!, s])));

  /** Replacements chosen in this screen, by block index. They apply to the sets not recorded yet */
  private replacements = signal<Record<number, Replacement>>({});

  private planItems = computed<WorkoutItem[]>(() =>
    this.blocks().map(block => ({ key: `plan-${block.index}`, kind: 'plan' as const, block })));

  /** Extra sets of each exercise, in the order they were recorded */
  private extraSetsByExercise = computed(() => {
    const byExercise = new Map<string, PerformedSet[]>();
    for (const set of this.performed().filter(s => !s.templateSetId)) {
      byExercise.set(set.exerciseId, [...(byExercise.get(set.exerciseId) ?? []), set]);
    }
    return byExercise;
  });

  /** Extra sets of an exercise that is planned go in its own table (the last block performed with it) */
  private planOwnerOf(exerciseId: string): string | null {
    return this.planItems().filter(i => i.kind === 'plan' && this.blockExercise(i.block).exerciseId === exerciseId).at(-1)?.key ?? null;
  }

  /** Planned exercises followed by the extra ones (with sets, or just added here) */
  items = computed<WorkoutItem[]>(() => {
    const extras: WorkoutItem[] = [];
    for (const [exerciseId, sets] of this.extraSetsByExercise()) {
      if (this.planOwnerOf(exerciseId)) continue;
      const group: ExtraGroup = { exerciseId, exerciseName: sets[0].exerciseName || 'Ejercicio', sets };
      extras.push({ key: `extra-${exerciseId}`, kind: 'extra', group });
    }
    for (const added of this.drafts().added) {
      if (this.extraSetsByExercise().has(added.exerciseId) || this.planOwnerOf(added.exerciseId)) continue;
      extras.push({ key: `extra-${added.exerciseId}`, kind: 'extra', group: { ...added, sets: [] } });
    }
    return [...this.planItems(), ...extras];
  });

  // ---------------------------------------------------------------- Supersets

  /** Supersets decided in this screen. Saved in the browser so an improvised superset survives a reload */
  private overrides = signal<SuperSetOverrides>({});

  /** Superset an exercise is performed in: decided here, the one of its recorded sets, or the planned one */
  groupOf(item: WorkoutItem): string | null {
    const overrides = this.overrides();
    if (item.key in overrides) return overrides[item.key];
    const recorded = this.recordedSets(item);
    if (recorded.length) return recorded[recorded.length - 1].superSetGroupId;
    return item.kind === 'plan' ? item.block.plannedGroupId : null;
  }

  units = computed<SuperSetUnit<WorkoutItem>[]>(() => buildUnits(this.items(), item => this.groupOf(item)));
  /** Planned exercises and every superset (a superset with an extra exercise stays with the planned ones) */
  private mainUnits = computed(() => this.units().filter(u => u.kind === 'superset' || u.item.kind === 'plan'));
  /** Extra exercises performed alone */
  private extraItems = computed(() => this.units()
    .filter(u => u.kind === 'single' && u.item.kind === 'extra')
    .map(u => (u as { item: WorkoutItem }).item));

  /** Selection of exercises to join in an improvised superset */
  isSelecting = signal(false);
  selectedKeys = signal<string[]>([]);
  isSavingSuperSet = signal(false);
  canJoin = computed(() => {
    const selected = this.items().filter(i => this.selectedKeys().includes(i.key));
    return new globalThis.Set(selected.map(i => this.itemExercise(i).exerciseId)).size >= 2;
  });

  // ---------------------------------------------------------------- Dialogs and state of the rows

  isStartOpen = signal(false);
  isStarting = signal(false);
  isConfirmingFinish = signal(false);
  isFinishing = signal(false);
  isConfirmingDiscard = signal(false);
  isDiscarding = signal(false);
  /** Number of the week this session has just completed (all its sessions are done), to congratulate the user */
  completedWeek = signal<number | null>(null);
  /** What the exercise picker is open for: replacing the exercise of an item or adding an extra one */
  picker = signal<{ mode: 'replace'; key: string } | { mode: 'extra' } | null>(null);
  /** Open ⋯ menu: "session" or the key of an exercise */
  openMenu = signal<string | null>(null);

  drafts = signal<StoredDrafts>(EMPTY_DRAFTS);
  pending = signal<Record<string, PendingChange>>({});
  private retryTimer: ReturnType<typeof setTimeout> | null = null;

  sheet = signal<{ row: SetRow; field: SetField; initial: DraftValues; done: boolean } | null>(null);
  rest = signal<RestStart | null>(null);

  constructor() {
    // What is typed is kept in the browser while the session is in progress
    effect(() => {
      const drafts = this.drafts();
      const session = this.session();
      if (session && !session.finishedAt) this.storeDrafts(session.id, drafts);
    });
    inject(DestroyRef).onDestroy(() => { if (this.retryTimer) clearTimeout(this.retryTimer); });
  }

  async ngOnInit() {
    const templateId = this.route.snapshot.paramMap.get('templateid')!;
    this.templateSessionId = this.route.snapshot.paramMap.get('sessionid')!;
    try {
      // The sessions are reloaded too so the name is there when the page is opened directly
      await Promise.all([
        this.store.GetSetsFromASessionTemplate(templateId, this.templateSessionId),
        this.store.GetSessionWorkout(this.templateSessionId),
        this.templateSession() ? Promise.resolve(true) : this.store.GetSessionsFromATrainingTemplate(templateId),
      ]);
      this.loadOverrides();
      this.loadDrafts();
      const exerciseIds = [
        ...this.store.sets().map(s => s.exerciseId),
        ...this.performed().map(s => s.exerciseId),
        ...this.drafts().added.map(a => a.exerciseId),
      ];
      await this.store.GetPreviousPerformance(exerciseIds, this.session()?.id ?? null, true);
    } finally {
      this.isLoading.set(false);
    }
  }

  // ---------------------------------------------------------------- Header

  startedAtLabel = computed(() => {
    const session = this.session();
    return session ? formatTime(session.startAt) : '';
  });

  durationLabel = computed(() => {
    const session = this.session();
    return session?.finishedAt ? formatDuration(session.duration) : '';
  });

  async startSession(form: StartSessionForm) {
    this.isStarting.set(true);
    try {
      const started = await this.store.StartTrainingSession(
        this.templateSessionId, form.motivationLevel, form.sleepLevel, form.domsLevel, form.comments);
      if (started) {
        this.isStartOpen.set(false);
        this.loadOverrides();
        this.loadDrafts();
      }
    } finally {
      this.isStarting.set(false);
    }
  }

  askFinish() {
    this.openMenu.set(null);
    this.isConfirmingFinish.set(true);
  }

  async finishSession() {
    const session = this.session();
    if (!session || this.hasPending()) return;
    this.isFinishing.set(true);
    try {
      const result = await this.store.FinishTrainingSession(this.templateSessionId, session.id);
      if (result) {
        this.rest.set(null);
        this.removeStored(DRAFTS_STORAGE_KEY, session.id);
        this.drafts.set(EMPTY_DRAFTS);
        if (result.weekFinished) this.completedWeek.set(result.weekNumber);
      }
    } finally {
      this.isFinishing.set(false);
      this.isConfirmingFinish.set(false);
    }
  }

  askDiscard() {
    this.openMenu.set(null);
    this.isConfirmingDiscard.set(true);
  }

  /** Deletes the session in progress with all its sets; the plan stays and it can be started again */
  async discardSession() {
    const session = this.session();
    if (!session) return;
    this.isDiscarding.set(true);
    try {
      if (await this.store.DiscardTrainingSession(this.templateSessionId, session.id)) {
        this.replacements.set({});
        this.overrides.set({});
        this.drafts.set(EMPTY_DRAFTS);
        this.pending.set({});
        this.rest.set(null);
        this.removeStored(OVERRIDES_STORAGE_KEY, session.id);
        this.removeStored(DRAFTS_STORAGE_KEY, session.id);
      }
    } finally {
      this.isDiscarding.set(false);
      this.isConfirmingDiscard.set(false);
    }
  }

  // ---------------------------------------------------------------- Items

  /** Exercise performed for the pending sets of a block: the one chosen here, the one already used today or the planned one */
  private blockExercise(block: PlannedBlock): Replacement {
    const replacement = this.replacements()[block.index];
    if (replacement) return replacement;
    const used = block.sets
      .map(s => this.performedByPlannedSet().get(s.id))
      .filter(p => p !== undefined)
      .at(-1);
    return used
      ? { exerciseId: used.exerciseId, exerciseName: used.exerciseName || block.exerciseName }
      : { exerciseId: block.exerciseId, exerciseName: block.exerciseName };
  }

  itemExercise(item: WorkoutItem): Replacement {
    return item.kind === 'plan'
      ? this.blockExercise(item.block)
      : { exerciseId: item.group.exerciseId, exerciseName: item.group.exerciseName };
  }

  private recordedSets(item: WorkoutItem): PerformedSet[] {
    return item.kind === 'plan'
      ? item.block.sets.map(s => this.performedByPlannedSet().get(s.id)).filter(p => p !== undefined)
      : item.group.sets;
  }

  private previousOf(exerciseId: string, index: number): RowValues | null {
    const set = this.store.previousPerformance()[exerciseId]?.sets[index];
    return set ? valuesOf(set) : null;
  }

  /** Performed extra sets shown in the table of an item */
  private extraSetsOf(item: WorkoutItem): PerformedSet[] {
    if (item.kind === 'extra') return item.group.sets;
    const exerciseId = this.blockExercise(item.block).exerciseId;
    return this.planOwnerOf(exerciseId) === item.key ? this.extraSetsByExercise().get(exerciseId) ?? [] : [];
  }

  /** Rows of an item: its planned sets, then its extra sets (recorded, then empty ones added here) */
  rowsOf(item: WorkoutItem): SetRow[] {
    const exercise = this.itemExercise(item);
    const rows: SetRow[] = [];
    let prior: RowValues | null = null;
    const push = (row: Omit<SetRow, 'prior' | 'label'>) => {
      rows.push({ ...row, prior, label: null });
      const view = this.valuesShown(rows.at(-1)!);
      if (view) prior = view;
    };

    if (item.kind === 'plan') {
      item.block.sets.forEach((planned, i) => {
        const performed = this.performedByPlannedSet().get(planned.id) ?? null;
        const exerciseId = performed?.exerciseId ?? exercise.exerciseId;
        push({
          key: `p-${planned.id}`, ownerKey: item.key, position: i + 1,
          exerciseId, exerciseName: performed ? performed.exerciseName || exercise.exerciseName : exercise.exerciseName,
          planned, performed, previous: this.previousOf(exerciseId, i), target: targetOf(planned),
        });
      });
    }
    const base = rows.length;
    const extras = this.extraSetsOf(item);
    extras.forEach((performed, j) => push({
      key: `x-${item.key}-${j}`, ownerKey: item.key, position: base + j + 1,
      exerciseId: performed.exerciseId, exerciseName: performed.exerciseName || exercise.exerciseName,
      planned: null, performed, previous: this.previousOf(performed.exerciseId, base + j), target: null,
    }));
    const empty = this.canRecord() ? this.drafts().extraRows[item.key] ?? 0 : 0;
    for (let d = 0; d < empty; d++) {
      const j = extras.length + d;
      push({
        key: `x-${item.key}-${j}`, ownerKey: item.key, position: base + j + 1,
        exerciseId: exercise.exerciseId, exerciseName: exercise.exerciseName,
        planned: null, performed: null, previous: this.previousOf(exercise.exerciseId, base + j), target: null,
      });
    }
    return rows;
  }

  /** Values of a row that is done (saved or on its way), or null */
  private valuesShown(row: SetRow): RowValues | null {
    const change = this.pending()[row.key];
    if (change?.op === 'remove') return null;
    if (change) return change.values;
    return row.performed ? valuesOf(row.performed) : null;
  }

  viewOf(row: SetRow): RowView {
    const change = this.pending()[row.key];
    const shown = this.valuesShown(row);
    const done = shown !== null;
    const typed: DraftValues = shown ?? this.drafts().values[row.key] ?? {};
    const isBodyWeight = typed.isBodyWeight ?? row.previous?.isBodyWeight ?? row.prior?.isBodyWeight ?? false;
    const hintWeight = row.previous?.weight ?? row.prior?.weight;
    const hintReps = row.target ? `${row.target.min}–${row.target.max}` : row.previous ? String(row.previous.repetitions) : '—';
    const hintRpe = row.target ? formatNumber(row.target.rpe) : '—';
    return {
      done,
      status: change && change.op !== 'remove' ? change.status : null,
      weight: typed.weight !== undefined ? formatLoad(typed.weight, isBodyWeight) : hintWeight !== undefined ? formatLoad(hintWeight, isBodyWeight) : '—',
      repetitions: typed.repetitions !== undefined ? String(typed.repetitions) : hintReps,
      rpe: typed.rpe !== undefined ? formatNumber(typed.rpe) : hintRpe,
      hint: { weight: typed.weight === undefined, repetitions: typed.repetitions === undefined, rpe: typed.rpe === undefined },
      previous: row.previous ? formatShortSet(row.previous) : '—',
    };
  }

  private entry(row: SetRow): TableEntry {
    return { kind: 'row', key: row.key, row, view: this.viewOf(row) };
  }

  /** Rows of one exercise with a divider where the target changes and before the extra sets */
  private exerciseEntries(item: WorkoutItem): TableEntry[] {
    const rows = this.rowsOf(item);
    const planned = rows.filter(r => r.target);
    const mixed = targetTranches(planned.map(r => r.target!)).length > 1;
    const entries: TableEntry[] = [];
    let lastTarget = '';
    for (const row of rows) {
      if (row.target && mixed) {
        const label = targetLabel(row.target);
        if (label !== lastTarget) entries.push({ kind: 'divider', key: `${row.key}-target`, label, tone: 'target' });
        lastTarget = label;
      }
      if (!row.target && planned.length && !entries.some(e => e.kind === 'divider' && e.tone === 'extra')) {
        entries.push({ kind: 'divider', key: `${item.key}-extra`, label: 'Series extra', tone: 'extra' });
      }
      entries.push(this.entry(row));
    }
    return entries;
  }

  private heading(item: WorkoutItem, label: string | null): ExerciseHeading {
    const exercise = this.itemExercise(item);
    const isPlan = item.kind === 'plan';
    return {
      key: item.key,
      label,
      name: exercise.exerciseName,
      replacedFrom: isPlan && exercise.exerciseId !== item.block.exerciseId ? item.block.exerciseName : null,
      scheme: isPlan ? schemeOf(item.block.sets.map(targetOf)) : 'Fuera del plan',
      isPlan,
    };
  }

  private isItemComplete(item: WorkoutItem): boolean {
    const planned = this.rowsOf(item).filter(r => r.planned);
    return planned.length > 0 && planned.every(r => this.valuesShown(r) !== null);
  }

  /** The cards of the list: each exercise with its table, and each superset with its rounds */
  cards = computed<SessionCard[]>(() => {
    let number = 0;
    return this.mainUnits().map(unit => {
      if (unit.kind === 'single') {
        return { kind: 'exercise', key: unit.item.key, number: ++number, heading: this.heading(unit.item, null),
          entries: this.exerciseEntries(unit.item), complete: this.isItemComplete(unit.item), item: unit.item };
      }
      const rowsByMember = unit.items.map(item => this.rowsOf(item));
      const rounds = Math.max(...rowsByMember.map(r => r.length));
      const entries: TableEntry[] = [];
      let currentRound: number | null = null;
      for (let r = 0; r < rounds; r++) {
        entries.push({ kind: 'divider', key: `${unit.groupId}-round-${r}`, label: `Ronda ${r + 1}`, tone: 'round' });
        rowsByMember.forEach((rows, m) => {
          const row = rows[r];
          if (!row) return;
          if (currentRound === null && this.valuesShown(row) === null) currentRound = r + 1;
          entries.push(this.entry({ ...row, label: `${unit.label}${m + 1}` }));
        });
      }
      return {
        kind: 'superset', key: unit.groupId, label: unit.label, unit,
        members: unit.items.map((item, m) => this.heading(item, `${unit.label}${m + 1}`)),
        entries,
        progress: currentRound ? `ronda ${currentRound} de ${rounds}` : 'completada',
      };
    });
  });

  extraCards = computed<SessionCard[]>(() => this.extraItems().map(item => ({
    kind: 'exercise', key: item.key, number: null, heading: this.heading(item, null),
    entries: this.exerciseEntries(item), complete: false, item,
  })));

  /** Every row of the session, for the progress and the finish */
  private allRows = computed(() => this.items().flatMap(item => this.rowsOf(item)));
  progress = computed(() => {
    const rows = this.allRows();
    const counted = rows.filter(r => r.planned || this.valuesShown(r) !== null);
    const items = this.items().filter(i => i.kind === 'plan');
    return {
      done: counted.filter(r => this.valuesShown(r) !== null).length,
      total: counted.length,
      exercisesDone: items.filter(i => this.isItemComplete(i)).length,
      exercises: items.length,
    };
  });
  /** Planned sets not marked: they will not be saved */
  uncheckedPlanned = computed(() => this.allRows().filter(r => r.planned && this.valuesShown(r) === null).length);
  pendingCount = computed(() => Object.values(this.pending()).filter(c => c.op !== 'remove').length);
  hasPending = computed(() => Object.keys(this.pending()).length > 0);
  unsavedCount = computed(() => Object.values(this.pending()).filter(c => c.op !== 'remove' && c.status === 'unsaved').length);

  finishMessage = computed(() => {
    const unchecked = this.uncheckedPlanned();
    const base = 'Se guardará la duración y ya no podrás registrar ni corregir series de esta sesión. Si era la última sesión pendiente, la semana se cerrará.';
    return unchecked > 0
      ? `Quedan ${unchecked} ${unchecked === 1 ? 'serie planificada sin marcar, que no se guardará' : 'series planificadas sin marcar, que no se guardarán'}. ${base}`
      : base;
  });

  // ---------------------------------------------------------------- Marking sets

  /** ✓ of a row: marks it done with what is shown (typed or hinted), or undoes it */
  async toggle(row: SetRow) {
    if (!this.canRecord()) return;
    const change = this.pending()[row.key];
    if (change?.status === 'saving') return;
    if (this.valuesShown(row) !== null) {
      await this.undo(row);
      return;
    }
    const values = defaultValues(row, this.drafts().values[row.key]);
    const complete = completeValues(values);
    if (!complete) {
      this.openSheet(row, values.weight === undefined ? 'weight' : values.repetitions === undefined ? 'repetitions' : 'rpe');
      return;
    }
    await this.register(row, complete);
  }

  private async register(row: SetRow, values: RowValues) {
    const attempts = this.pending()[row.key]?.op === 'register' ? (this.pending()[row.key] as { attempts: number }).attempts : 0;
    this.setPending(row.key, { op: 'register', row, values, status: 'saving', attempts });
    this.rest.set({ at: Date.now(), label: `${row.exerciseName} · serie ${row.position}` });
    await this.send(row.key);
  }

  private async update(row: SetRow, values: RowValues) {
    this.setPending(row.key, { op: 'update', row, values, status: 'saving', attempts: 0 });
    await this.send(row.key);
  }

  /** Sends a pending register or update; if it fails, it stays unsaved and is retried later */
  private async send(key: string) {
    const change = this.pending()[key];
    const session = this.session();
    if (!change || change.op === 'remove' || !session) return;
    const { row, values } = change;
    const owner = this.items().find(i => i.key === row.ownerKey);
    const extrasBefore = owner ? this.extraSetsOf(owner).length : 0;

    const saved = change.op === 'register'
      ? await this.store.RegisterSet(this.templateSessionId, session.id, row.planned?.id ?? null, owner ? this.groupOf(owner) : null,
          { exerciseId: row.exerciseId, ...values })
      : await this.store.UpdateSet(this.templateSessionId, session.id, row.performed!.id, { exerciseId: row.performed!.exerciseId, ...values });

    if (this.pending()[key] !== change) return; // replaced meanwhile (edited again or undone)
    if (saved) {
      this.clearPending(key);
      if (change.op === 'register') this.afterRegister(row, extrasBefore);
      return;
    }
    // The answer may be lost after saving it: a planned set can be checked in the reloaded workout
    if (change.op === 'register' && row.planned) {
      await this.store.GetSessionWorkout(this.templateSessionId);
      if (this.performedByPlannedSet().has(row.planned.id)) {
        this.clearPending(key);
        this.afterRegister(row, extrasBefore);
        return;
      }
    }
    this.setPending(key, { ...change, status: 'unsaved', attempts: change.attempts + 1 });
    // The row already says it is unsaved and it will be retried: no need for the error banner too
    this.store.ClearError();
    this.scheduleRetry();
  }

  /**
   * A recorded set no longer needs its draft. For an extra row, the empty rows of its exercise go down by one
   * and the ones typed before it move up one place, so every value stays in its row
   */
  private afterRegister(row: SetRow, extrasBefore: number) {
    this.drafts.update(d => {
      const values = { ...d.values };
      delete values[row.key];
      if (row.planned) return { ...d, values };
      const owner = row.ownerKey;
      const index = Number(row.key.split('-').at(-1));
      for (let k = index - 1; k >= extrasBefore; k--) {
        const from = `x-${owner}-${k}`;
        if (values[from]) values[`x-${owner}-${k + 1}`] = values[from];
        else delete values[`x-${owner}-${k + 1}`];
      }
      delete values[`x-${owner}-${extrasBefore}`];
      return { ...d, values, extraRows: { ...d.extraRows, [owner]: Math.max(0, (d.extraRows[owner] ?? 0) - 1) } };
    });
  }

  /** Undoes a done row: an unsaved one just goes back to being a draft; a saved one is deleted keeping its values */
  private async undo(row: SetRow) {
    const change = this.pending()[row.key];
    const shown = this.valuesShown(row)!;
    if (change && change.op !== 'remove') {
      this.clearPending(row.key);
      if (!row.performed) {
        this.setDraft(row.key, shown);
        return;
      }
    }
    const performed = row.performed;
    const session = this.session();
    if (!performed || !session) return;
    const owner = this.items().find(i => i.key === row.ownerKey);
    const extrasBefore = owner ? this.extraSetsOf(owner).length : 0;
    this.setDraft(row.key, valuesOf(performed));
    this.setPending(row.key, { op: 'remove', status: 'saving' });
    const removed = await this.store.RemoveSet(this.templateSessionId, session.id, performed.id);
    this.clearPending(row.key);
    if (!removed) {
      this.setDraft(row.key, undefined);
      return;
    }
    if (!row.planned) this.afterExtraRemoved(row, extrasBefore, valuesOf(performed));
  }

  /** A deleted extra set becomes the last empty row of its exercise, with its values; the rows after it move up */
  private afterExtraRemoved(row: SetRow, extrasBefore: number, removedValues: RowValues) {
    const owner = row.ownerKey;
    const index = Number(row.key.split('-').at(-1));
    this.drafts.update(d => {
      const values = { ...d.values };
      const empty = d.extraRows[owner] ?? 0;
      delete values[row.key];
      for (let k = index + 1; k < extrasBefore + empty; k++) {
        const from = `x-${owner}-${k}`;
        if (values[from]) values[`x-${owner}-${k - 1}`] = values[from];
        else delete values[`x-${owner}-${k - 1}`];
        delete values[from];
      }
      values[`x-${owner}-${extrasBefore - 1 + empty}`] = removedValues;
      return { ...d, values, extraRows: { ...d.extraRows, [owner]: empty + 1 } };
    });
  }

  private scheduleRetry() {
    if (this.retryTimer) return;
    const attempts = Math.max(1, ...Object.values(this.pending()).map(c => c.op === 'remove' ? 0 : c.attempts));
    const delay = Math.min(30_000, 2_000 * 2 ** (attempts - 1));
    this.retryTimer = setTimeout(() => {
      this.retryTimer = null;
      this.retryUnsaved();
    }, delay);
  }

  /** Sends again every unsaved change (also when the connection comes back) */
  retryUnsaved() {
    for (const [key, change] of Object.entries(this.pending())) {
      if (change.op === 'remove' || change.status !== 'unsaved') continue;
      this.setPending(key, { ...change, status: 'saving' });
      void this.send(key);
    }
  }

  private setPending(key: string, change: PendingChange) {
    this.pending.update(p => ({ ...p, [key]: change }));
  }

  private clearPending(key: string) {
    this.pending.update(p => {
      const next = { ...p };
      delete next[key];
      return next;
    });
  }

  private setDraft(key: string, values: DraftValues | undefined) {
    this.drafts.update(d => {
      const next = { ...d.values };
      if (values) next[key] = values;
      else delete next[key];
      return { ...d, values: next };
    });
  }

  // ---------------------------------------------------------------- Sheet of a set and rest

  openSheet(row: SetRow, field: SetField) {
    if (!this.canRecord()) return;
    const shown = this.valuesShown(row);
    this.openMenu.set(null);
    this.sheet.set({
      row, field,
      initial: shown ?? defaultValues(row, this.drafts().values[row.key]),
      done: shown !== null,
    });
  }

  async saveSheet(values: DraftValues) {
    const sheet = this.sheet();
    const complete = completeValues(values);
    this.sheet.set(null);
    if (!sheet || !complete) return;
    const { row } = sheet;
    // Look the row up again: the workout may have been reloaded while the sheet was open
    const current = this.allRows().find(r => r.key === row.key) ?? row;
    const change = this.pending()[row.key];
    if (change && change.op !== 'remove') {
      this.setPending(row.key, { ...change, values: complete, status: 'saving' });
      await this.send(row.key);
    } else if (current.performed) {
      await this.update(current, complete);
    } else {
      this.setDraft(row.key, undefined);
      await this.register(current, complete);
    }
  }

  /** Closing the sheet of a row not done keeps what was typed */
  closeSheet(values: DraftValues) {
    const sheet = this.sheet();
    this.sheet.set(null);
    if (sheet && !sheet.done) this.setDraft(sheet.row.key, values);
  }

  // ---------------------------------------------------------------- Menus, replacing and adding

  toggleMenu(key: string) {
    this.openMenu.update(open => open === key ? null : key);
  }

  onDocumentClick(event: MouseEvent) {
    if (!this.openMenu()) return;
    const inside = event.composedPath().some(node => node instanceof HTMLElement && node.dataset['menuRoot'] !== undefined);
    if (!inside) this.openMenu.set(null);
  }

  /** One more empty set at the end of an exercise (an extra set) */
  addSet(key: string) {
    this.openMenu.set(null);
    this.drafts.update(d => ({ ...d, extraRows: { ...d.extraRows, [key]: (d.extraRows[key] ?? 0) + 1 } }));
  }

  async openPicker(mode: 'replace' | 'extra', key?: string) {
    this.openMenu.set(null);
    if (this.exercises().length === 0) await this.store.GetAllExercises();
    this.picker.set(mode === 'replace' && key ? { mode, key } : { mode: 'extra' });
  }

  pickerExcludedId = computed(() => {
    const picker = this.picker();
    if (picker?.mode !== 'replace') return null;
    const item = this.items().find(i => i.key === picker.key);
    return item ? this.itemExercise(item).exerciseId : null;
  });

  async pickExercise(exercise: Exercise) {
    const picker = this.picker();
    this.picker.set(null);
    const choice = { exerciseId: exercise.id, exerciseName: exercise.name };

    if (picker?.mode === 'replace') {
      const item = this.items().find(i => i.key === picker.key);
      if (item?.kind === 'plan') this.replacements.update(r => ({ ...r, [item.block.index]: choice }));
    } else if (picker?.mode === 'extra') {
      // An exercise already planned gets one more set in its table; any other is a new extra exercise with one set
      const key = this.planOwnerOf(exercise.id) ?? `extra-${exercise.id}`;
      this.drafts.update(d => ({
        ...d,
        added: key.startsWith('extra-') && !d.added.some(a => a.exerciseId === exercise.id) ? [...d.added, choice] : d.added,
        extraRows: { ...d.extraRows, [key]: (d.extraRows[key] ?? 0) + 1 },
      }));
    }
    await this.store.GetPreviousPerformance([exercise.id], this.session()?.id ?? null);
  }

  /** The pending sets of a block go back to the planned exercise (the recorded ones keep theirs) */
  restorePlanned(key: string) {
    this.openMenu.set(null);
    const item = this.items().find(i => i.key === key);
    if (item?.kind !== 'plan') return;
    const block = item.block;
    this.replacements.update(r => ({ ...r, [block.index]: { exerciseId: block.exerciseId, exerciseName: block.exerciseName } }));
  }

  // ---------------------------------------------------------------- Improvised supersets

  startSelecting() {
    this.openMenu.set(null);
    this.selectedKeys.set([]);
    this.isSelecting.set(true);
  }

  cancelSelecting() {
    this.isSelecting.set(false);
    this.selectedKeys.set([]);
  }

  isSelected(key: string): boolean {
    return this.selectedKeys().includes(key);
  }

  toggleSelected(key: string) {
    this.selectedKeys.update(keys => keys.includes(key) ? keys.filter(k => k !== key) : [...keys, key]);
  }

  /** Joins the selected exercises in a new superset; the sets already recorded are moved to it too */
  async joinSelected() {
    const session = this.session();
    if (!session || !this.canJoin()) return;
    const selected = this.items().filter(i => this.selectedKeys().includes(i.key));
    await this.applySuperSet(session.id, selected, crypto.randomUUID());
    this.cancelSelecting();
  }

  /** Separates a superset: its exercises are performed alone from now on */
  async separate(unit: SuperSetUnit<WorkoutItem> & { kind: 'superset' }) {
    const session = this.session();
    if (!session) return;
    await this.applySuperSet(session.id, unit.items, null);
  }

  private async applySuperSet(sessionId: string, items: WorkoutItem[], groupId: string | null) {
    this.overrides.update(o => {
      const next = { ...o };
      for (const item of items) next[item.key] = groupId;
      return next;
    });
    this.storeValue(OVERRIDES_STORAGE_KEY, sessionId, this.overrides());
    const setIds = items.flatMap(i => [...this.recordedSets(i), ...(i.kind === 'plan' ? this.extraSetsOf(i) : [])].map(s => s.id));
    if (setIds.length === 0) return;
    this.isSavingSuperSet.set(true);
    try {
      await this.store.AssignSuperSet(this.templateSessionId, sessionId, setIds, groupId);
    } finally {
      this.isSavingSuperSet.set(false);
    }
  }

  // ---------------------------------------------------------------- Browser storage
  // It is only a convenience: it may be unavailable, so every access is guarded

  private loadOverrides() {
    const sessionId = this.session()?.id;
    this.overrides.set(sessionId ? this.readStored<SuperSetOverrides>(OVERRIDES_STORAGE_KEY, sessionId) ?? {} : {});
  }

  private loadDrafts() {
    const sessionId = this.session()?.id;
    const stored = sessionId ? this.readStored<StoredDrafts>(DRAFTS_STORAGE_KEY, sessionId) : null;
    this.drafts.set(stored ? { ...EMPTY_DRAFTS, ...stored } : EMPTY_DRAFTS);
  }

  private storeDrafts(sessionId: string, drafts: StoredDrafts) {
    this.storeValue(DRAFTS_STORAGE_KEY, sessionId, drafts);
  }

  private readStored<T>(prefix: string, sessionId: string): T | null {
    try {
      const stored = localStorage.getItem(prefix + sessionId);
      return stored ? JSON.parse(stored) as T : null;
    } catch {
      return null;
    }
  }

  private storeValue(prefix: string, sessionId: string, value: unknown) {
    try {
      localStorage.setItem(prefix + sessionId, JSON.stringify(value));
    } catch {
      // Without storage it only lasts until the page is reloaded
    }
  }

  private removeStored(prefix: string, sessionId: string) {
    try {
      localStorage.removeItem(prefix + sessionId);
    } catch {
      // Nothing to clean
    }
  }
}
