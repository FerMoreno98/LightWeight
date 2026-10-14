import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NgTemplateOutlet } from '@angular/common';
import { TrainingStore } from '../../../state/training.store';
import { Exercise, PerformedSet } from '../../../data/training-api.service';
import { TECHNIQUE_LABELS } from '../../shared/cycle-format';
import { SuperSetUnit, buildUnits, groupConsecutive } from '../../shared/superset';
import { ConfirmDialog } from '../../../../../Shared/confirm-dialog/confirm-dialog';
import { StartSessionDialog, StartSessionForm } from '../start-session-dialog/start-session-dialog';
import { ExercisePicker } from '../exercise-picker/exercise-picker';
import { ExerciseFocus } from '../exercise-focus/exercise-focus';
import {
  ExtraGroup, FocusSlot, FocusSuperSet, FocusTarget, FocusView, PlannedBlock, SetForm, WorkoutItem,
  formatDuration, formatPerformedSet, formatTime,
} from '../session-workout.model';

/** Exercise chosen in this screen to replace the planned one of a block */
interface Replacement {
  exerciseId: string;
  exerciseName: string;
}

/** Superset of each exercise (by item key) decided in this screen; null = performed alone */
type SuperSetOverrides = Record<string, string | null>;

const OVERRIDES_STORAGE_KEY = 'lightweight.session-supersets.';

/**
 * A session of the week: its plan, the button to start it and, once started, the list of exercises with their
 * progress. Tapping an exercise opens the focus mode to record its sets. Exercises can be performed in supersets,
 * planned in the template or improvised here
 */
@Component({
  selector: 'app-session-plan',
  standalone: true,
  imports: [RouterLink, NgTemplateOutlet, ConfirmDialog, StartSessionDialog, ExercisePicker, ExerciseFocus],
  templateUrl: './session-plan.html',
  styleUrl: './session-plan.css',
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

  extraGroups = computed<ExtraGroup[]>(() => {
    const groups: ExtraGroup[] = [];
    for (const set of this.performed().filter(s => !s.templateSetId)) {
      const group = groups.find(g => g.exerciseId === set.exerciseId);
      if (group) group.sets.push(set);
      else groups.push({ exerciseId: set.exerciseId, exerciseName: set.exerciseName || 'Ejercicio', sets: [set] });
    }
    return groups;
  });

  /** Planned exercises followed by the extra ones */
  items = computed<WorkoutItem[]>(() => [
    ...this.blocks().map(block => ({ key: `plan-${block.index}`, kind: 'plan' as const, block })),
    ...this.extraGroups().map(group => ({ key: `extra-${group.exerciseId}`, kind: 'extra' as const, group })),
  ]);

  /** Replacements chosen in this screen, by block index. They apply to the sets not recorded yet */
  private replacements = signal<Record<number, Replacement>>({});

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
  mainUnits = computed(() => this.units().filter(u => u.kind === 'superset' || u.item.kind === 'plan'));
  /** Extra exercises performed alone */
  extraItems = computed(() => this.units()
    .filter(u => u.kind === 'single' && u.item.kind === 'extra')
    .map(u => (u as { item: WorkoutItem }).item));

  private supersetOf(item: WorkoutItem): Extract<SuperSetUnit<WorkoutItem>, { kind: 'superset' }> | null {
    const unit = this.units().find(u => u.kind === 'superset' && u.items.some(i => i.key === item.key));
    return unit?.kind === 'superset' ? unit : null;
  }

  /** Selection of exercises to join in an improvised superset */
  isSelecting = signal(false);
  selectedKeys = signal<string[]>([]);
  isSavingSuperSet = signal(false);
  canJoin = computed(() => {
    const selected = this.items().filter(i => this.selectedKeys().includes(i.key));
    return new globalThis.Set(selected.map(i => this.itemExercise(i).exerciseId)).size >= 2;
  });

  // ---------------------------------------------------------------- Dialogs and focus

  focus = signal<FocusTarget | null>(null);
  isStartOpen = signal(false);
  isStarting = signal(false);
  isConfirmingFinish = signal(false);
  isFinishing = signal(false);
  isConfirmingDiscard = signal(false);
  isDiscarding = signal(false);
  /** Number of the week this session has just completed (all its sessions are done), to congratulate the user */
  completedWeek = signal<number | null>(null);
  isSaving = signal(false);
  /** What the exercise picker is open for: replacing the exercise in focus or adding an extra one */
  pickerMode = signal<'replace' | 'extra' | null>(null);

  readonly formatPerformedSet = formatPerformedSet;

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
      const exerciseIds = [
        ...this.store.sets().map(s => s.exerciseId),
        ...this.performed().map(s => s.exerciseId),
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
        if (this.blocks().length) this.focus.set({ kind: 'plan', block: 0, set: 0 });
      }
    } finally {
      this.isStarting.set(false);
    }
  }

  async finishSession() {
    const session = this.session();
    if (!session) return;
    this.isFinishing.set(true);
    try {
      const result = await this.store.FinishTrainingSession(this.templateSessionId, session.id);
      if (result) {
        this.focus.set(null);
        if (result.weekFinished) this.completedWeek.set(result.weekNumber);
      }
    } finally {
      this.isFinishing.set(false);
      this.isConfirmingFinish.set(false);
    }
  }

  /** Deletes the session in progress with all its sets; the plan stays and it can be started again */
  async discardSession() {
    const session = this.session();
    if (!session) return;
    this.isDiscarding.set(true);
    try {
      if (await this.store.DiscardTrainingSession(this.templateSessionId, session.id)) {
        this.focus.set(null);
        this.replacements.set({});
        this.overrides.set({});
        this.removeStoredOverrides(session.id);
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

  /** Set positions of an exercise: the planned ones, or the extra sets plus a new one while recording */
  private itemSlots(item: WorkoutItem): FocusSlot[] {
    if (item.kind === 'plan') {
      return item.block.sets.map(planned => ({ planned, performed: this.performedByPlannedSet().get(planned.id) ?? null }));
    }
    const slots: FocusSlot[] = item.group.sets.map(p => ({ planned: null, performed: p }));
    if (this.canRecord()) slots.push({ planned: null, performed: null });
    return slots;
  }

  private itemOfTarget(target: FocusTarget): WorkoutItem | null {
    if (target.kind === 'plan') {
      return this.items().find(i => i.kind === 'plan' && i.block.index === target.block) ?? null;
    }
    // An extra exercise just added has no sets yet, so it is not in the list
    return this.items().find(i => i.kind === 'extra' && i.group.exerciseId === target.exerciseId)
      ?? { key: `extra-${target.exerciseId}`, kind: 'extra', group: { exerciseId: target.exerciseId, exerciseName: target.exerciseName, sets: [] } };
  }

  private targetFor(item: WorkoutItem, set: number): FocusTarget {
    return item.kind === 'plan'
      ? { kind: 'plan', block: item.block.index, set }
      : { kind: 'extra', exerciseId: item.group.exerciseId, exerciseName: item.group.exerciseName, set };
  }

  doneCount(block: PlannedBlock): number {
    return block.sets.filter(s => this.performedByPlannedSet().has(s.id)).length;
  }

  /** Name shown for a block: "Press banca → Press con mancuernas" when it was replaced */
  replacementName(block: PlannedBlock): string | null {
    const exercise = this.blockExercise(block);
    return exercise.exerciseId !== block.exerciseId ? exercise.exerciseName : null;
  }

  /** "80 kg × 8 @ 8 (+2)": last time the exercise performed in the item was done */
  lastPerformance(item: WorkoutItem): string | null {
    const previous = this.store.previousPerformance()[this.itemExercise(item).exerciseId];
    if (!previous?.sets.length) return null;
    const more = previous.sets.length - 1;
    return `${formatPerformedSet(previous.sets[0])}${more > 0 ? ` (+${more})` : ''}`;
  }

  techniqueLabel(block: PlannedBlock): string | null {
    const technique = block.sets.find(s => s.technique)?.technique;
    return technique ? TECHNIQUE_LABELS[technique] : null;
  }

  /** "Ronda 2/3" of a superset: the first round with something pending */
  supersetProgress(unit: Extract<SuperSetUnit<WorkoutItem>, { kind: 'superset' }>): { round: number; rounds: number; done: boolean } {
    const rounds = this.roundsOf(unit.items);
    for (let r = 0; r < rounds; r++) {
      if (unit.items.some(item => this.isPending(item, r))) return { round: r + 1, rounds, done: false };
    }
    return { round: rounds, rounds, done: true };
  }

  openItem(item: WorkoutItem) {
    if (this.isSelecting()) {
      this.toggleSelected(item);
      return;
    }
    if (!this.session()) return;
    if (item.kind === 'plan') {
      const firstPending = item.block.sets.findIndex(s => !this.performedByPlannedSet().has(s.id));
      this.focus.set(this.targetFor(item, firstPending === -1 ? 0 : firstPending));
    } else {
      this.focus.set(this.targetFor(item, this.canRecord() ? item.group.sets.length : 0));
    }
  }

  /** Opens a superset in its first pending slot, round by round */
  openSuperSet(unit: Extract<SuperSetUnit<WorkoutItem>, { kind: 'superset' }>) {
    if (!this.session() || this.isSelecting()) return;
    const next = this.nextPendingSlot(unit.items, -1, unit.items.length - 1);
    const fallback = unit.items[0];
    this.focus.set(next ? this.targetFor(next.item, next.round) : this.targetFor(fallback, 0));
  }

  // ---------------------------------------------------------------- Rounds of a superset

  /** Rounds of a superset: the most sets planned (or recorded, for extras); only extras can always add one more */
  private roundsOf(members: WorkoutItem[]): number {
    const planned = members.filter(m => m.kind === 'plan').map(m => (m as { block: PlannedBlock }).block.sets.length);
    const recorded = members.filter(m => m.kind === 'extra').map(m => (m as { group: ExtraGroup }).group.sets.length);
    const onlyExtras = planned.length === 0;
    return Math.max(1, ...planned, ...recorded.map(r => onlyExtras && this.canRecord() ? r + 1 : r));
  }

  private isPending(item: WorkoutItem, round: number): boolean {
    if (item.kind === 'plan') {
      const planned = item.block.sets[round];
      return !!planned && !this.performedByPlannedSet().has(planned.id);
    }
    return this.canRecord() && round >= item.group.sets.length;
  }

  private isDone(item: WorkoutItem, round: number): boolean {
    return item.kind === 'plan'
      ? !!item.block.sets[round] && this.performedByPlannedSet().has(item.block.sets[round].id)
      : round < item.group.sets.length;
  }

  /**
   * Next pending slot of a superset after (round, member), going round by round (A1, A2, A1, A2...).
   * When nothing is pending after it, it starts again from the first round
   */
  private nextPendingSlot(members: WorkoutItem[], round: number, member: number, skip?: { round: number; member: number })
    : { item: WorkoutItem; round: number; member: number } | null {
    const rounds = this.roundsOf(members);
    const positions: { round: number; member: number }[] = [];
    for (let r = 0; r < rounds; r++) {
      for (let m = 0; m < members.length; m++) positions.push({ round: r, member: m });
    }
    const start = positions.findIndex(p => p.round === round && p.member === member) + 1;
    const ordered = [...positions.slice(start), ...positions.slice(0, start)];
    const next = ordered.find(p =>
      !(skip && p.round === skip.round && p.member === skip.member) && this.isPending(members[p.member], p.round));
    return next ? { item: members[next.member], round: next.round, member: next.member } : null;
  }

  // ---------------------------------------------------------------- Focus mode

  focusView = computed<FocusView | null>(() => {
    const target = this.focus();
    const session = this.session();
    if (!target || !session) return null;
    const item = this.itemOfTarget(target);
    if (!item) return null;
    const readOnly = !this.canRecord();
    const previous = this.store.previousPerformance();
    const slots = this.itemSlots(item);
    if (slots.length === 0) return null;
    const setIndex = Math.min(target.set, slots.length - 1);
    const inFocus = slots[setIndex].performed;
    const exercise = this.itemExercise(item);
    // A recorded set shows its own exercise; the pending ones the replacement (if any)
    const exerciseId = inFocus?.exerciseId ?? exercise.exerciseId;
    const exerciseName = inFocus ? inFocus.exerciseName || exercise.exerciseName : exercise.exerciseName;
    const items = this.items();
    const position = items.findIndex(i => i.key === item.key);

    const unit = this.supersetOf(item);
    let superSet: FocusSuperSet | null = null;
    let nextLabel: string | null = null;
    if (unit) {
      const member = unit.items.findIndex(i => i.key === item.key);
      superSet = {
        label: unit.label,
        round: setIndex + 1,
        rounds: Math.max(this.roundsOf(unit.items), setIndex + 1),
        members: unit.items.map((m, i) => ({
          label: `${unit.label}${i + 1}`,
          name: this.itemExercise(m).exerciseName,
          done: this.isDone(m, setIndex),
          current: m.key === item.key,
        })),
      };
      if (!inFocus && !readOnly) {
        const next = this.nextPendingSlot(unit.items, setIndex, member, { round: setIndex, member });
        nextLabel = next ? `${unit.label}${next.member + 1}` : null;
      }
    }

    const isPlan = item.kind === 'plan';
    return {
      key: `${item.key}-${setIndex}-${inFocus?.id ?? 'new'}-${exerciseId}-${previous[exerciseId] ? 1 : 0}`,
      kind: item.kind,
      exerciseId,
      exerciseName,
      replacedName: isPlan && exerciseId !== item.block.exerciseId ? item.block.exerciseName : null,
      canReplace: isPlan && !readOnly,
      canRestore: isPlan && !readOnly && exercise.exerciseId !== item.block.exerciseId,
      setIndex,
      slots,
      previous: previous[exerciseId] ?? null,
      readOnly,
      hasPreviousExercise: position > 0,
      hasNextExercise: position !== -1 && position < items.length - 1,
      superSet,
      nextLabel,
    };
  });

  selectSet(index: number) {
    const target = this.focus();
    if (target) this.focus.set({ ...target, set: index });
  }

  /** Goes to another exercise of the superset in focus, in the same round */
  selectMember(index: number) {
    const target = this.focus();
    const item = target ? this.itemOfTarget(target) : null;
    const unit = item ? this.supersetOf(item) : null;
    const member = unit?.items[index];
    if (!target || !member) return;
    const slots = this.itemSlots(member);
    this.focus.set(this.targetFor(member, Math.max(0, Math.min(target.set, slots.length - 1))));
  }

  /** Moves between the planned exercises and then the extra ones */
  moveExercise(delta: 1 | -1) {
    const target = this.focus();
    const item = target ? this.itemOfTarget(target) : null;
    if (!item) return;
    const items = this.items();
    const next = items[items.findIndex(i => i.key === item.key) + delta];
    if (next) this.openItem(next);
  }

  async saveSet(form: SetForm) {
    const view = this.focusView();
    const target = this.focus();
    const session = this.session();
    const item = target ? this.itemOfTarget(target) : null;
    if (!view || !target || !session || !item) return;
    const slot = view.slots[view.setIndex];
    const values = {
      exerciseId: view.exerciseId,
      weight: form.weight!,
      repetitions: form.repetitions!,
      rpe: form.rpe!,
      isBodyWeight: form.isBodyWeight,
    };
    // Correcting a recorded set keeps its exercise unless a replacement was chosen here
    if (slot.performed && target.kind === 'plan') {
      values.exerciseId = this.replacements()[target.block]?.exerciseId ?? slot.performed.exerciseId;
    }

    this.isSaving.set(true);
    try {
      const saved = slot.performed
        ? await this.store.UpdateSet(this.templateSessionId, session.id, slot.performed.id, values)
        : await this.store.RegisterSet(this.templateSessionId, session.id, slot.planned?.id ?? null, this.groupOf(item), values);
      if (saved && !slot.performed) this.advanceAfterRegister(item, view.setIndex);
    } finally {
      this.isSaving.set(false);
    }
  }

  /**
   * After recording a set the focus goes to the next pending set: in a superset, the next exercise of the round
   * (A1 → A2 → A1...); otherwise the next pending set of the same exercise
   */
  private advanceAfterRegister(item: WorkoutItem, setIndex: number) {
    // The workout has been reloaded, so the items are looked up again
    const current = this.items().find(i => i.key === item.key)
      ?? (item.kind === 'extra' ? { ...item, group: { ...item.group, sets: [] } } : item);
    const unit = this.supersetOf(current);
    if (unit) {
      const member = unit.items.findIndex(i => i.key === current.key);
      const next = this.nextPendingSlot(unit.items, setIndex, member);
      if (next) this.focus.set(this.targetFor(next.item, next.round));
      return;
    }
    if (current.kind === 'extra') {
      this.focus.set(this.targetFor(current, setIndex + 1));
      return;
    }
    const sets = current.block.sets;
    const pending = sets.findIndex((s, i) => i > setIndex && !this.performedByPlannedSet().has(s.id));
    const anyPending = sets.findIndex(s => !this.performedByPlannedSet().has(s.id));
    const next = pending !== -1 ? pending : anyPending;
    if (next !== -1) this.focus.set(this.targetFor(current, next));
  }

  async removeSet(set: PerformedSet) {
    const session = this.session();
    if (!session) return;
    this.isSaving.set(true);
    try {
      await this.store.RemoveSet(this.templateSessionId, session.id, set.id);
    } finally {
      this.isSaving.set(false);
    }
  }

  // ---------------------------------------------------------------- Improvised supersets

  startSelecting() {
    this.selectedKeys.set([]);
    this.isSelecting.set(true);
  }

  cancelSelecting() {
    this.isSelecting.set(false);
    this.selectedKeys.set([]);
  }

  isSelected(item: WorkoutItem): boolean {
    return this.selectedKeys().includes(item.key);
  }

  toggleSelected(item: WorkoutItem) {
    this.selectedKeys.update(keys => keys.includes(item.key) ? keys.filter(k => k !== item.key) : [...keys, item.key]);
  }

  /** Joins the selected exercises in a new superset; the sets already recorded are moved to it too */
  async joinSelected() {
    const session = this.session();
    if (!session || !this.canJoin()) return;
    const selected = this.items().filter(i => this.selectedKeys().includes(i.key));
    const groupId = crypto.randomUUID();
    await this.applySuperSet(session.id, selected, groupId);
    this.cancelSelecting();
  }

  /** Separates a superset: its exercises are performed alone from now on */
  async separate(unit: Extract<SuperSetUnit<WorkoutItem>, { kind: 'superset' }>) {
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
    this.saveOverrides(sessionId);
    const setIds = items.flatMap(i => this.recordedSets(i).map(s => s.id));
    if (setIds.length === 0) return;
    this.isSavingSuperSet.set(true);
    try {
      await this.store.AssignSuperSet(this.templateSessionId, sessionId, setIds, groupId);
    } finally {
      this.isSavingSuperSet.set(false);
    }
  }

  // The browser storage is only a convenience: it may be unavailable, so every access is guarded
  private loadOverrides() {
    const sessionId = this.session()?.id;
    if (!sessionId) return;
    try {
      const stored = localStorage.getItem(OVERRIDES_STORAGE_KEY + sessionId);
      this.overrides.set(stored ? JSON.parse(stored) as SuperSetOverrides : {});
    } catch {
      this.overrides.set({});
    }
  }

  private saveOverrides(sessionId: string) {
    try {
      localStorage.setItem(OVERRIDES_STORAGE_KEY + sessionId, JSON.stringify(this.overrides()));
    } catch {
      // Without storage the improvised superset only lasts until the page is reloaded
    }
  }

  private removeStoredOverrides(sessionId: string) {
    try {
      localStorage.removeItem(OVERRIDES_STORAGE_KEY + sessionId);
    } catch {
      // Nothing to clean
    }
  }

  // ---------------------------------------------------------------- Replacing and adding exercises

  async openPicker(mode: 'replace' | 'extra') {
    if (this.exercises().length === 0) await this.store.GetAllExercises();
    this.pickerMode.set(mode);
  }

  pickerExcludedId = computed(() => this.pickerMode() === 'replace' ? this.focusView()?.exerciseId ?? null : null);

  async pickExercise(exercise: Exercise) {
    const mode = this.pickerMode();
    const target = this.focus();
    this.pickerMode.set(null);
    const choice = { exerciseId: exercise.id, exerciseName: exercise.name };

    if (mode === 'replace' && target?.kind === 'plan') {
      const block = this.blocks()[target.block];
      if (exercise.id === block.exerciseId) {
        this.restorePlanned();
      } else {
        this.replacements.update(r => ({ ...r, [target.block]: choice }));
      }
    } else if (mode === 'extra') {
      const existing = this.extraGroups().find(g => g.exerciseId === exercise.id);
      this.focus.set({
        kind: 'extra',
        exerciseId: exercise.id,
        exerciseName: exercise.name,
        set: existing?.sets.length ?? 0,
      });
    }
    await this.store.GetPreviousPerformance([exercise.id], this.session()?.id ?? null);
  }

  /** The pending sets of the block go back to the planned exercise (the recorded ones keep theirs) */
  restorePlanned() {
    const target = this.focus();
    if (target?.kind !== 'plan') return;
    const block = this.blocks()[target.block];
    this.replacements.update(r => ({ ...r, [block.index]: { exerciseId: block.exerciseId, exerciseName: block.exerciseName } }));
  }
}
