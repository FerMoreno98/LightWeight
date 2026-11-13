import { Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';

/** Volume levels of the example, from the cool deload to the hot peak */
type Level = 'mv' | 'mev' | 'mav' | 'mrv';
type NodeId = 'prog' | 'tpl' | 'ses' | 'sets' | 'macro' | 'meso' | 'week' | 'train' | 'done';

interface MapStep {
  title: string;
  text: string;
}

const STEPS: MapStep[] = [
  { title: 'Primero diseñas', text: 'El programa, con una plantilla por cada nivel de volumen.' },
  { title: 'Luego lo ejecutas en el tiempo', text: 'Dentro de un macrociclo: bloque a bloque, semana a semana.' },
  { title: 'Cada nivel sigue a su pareja', text: 'El meso sigue un programa. Cada semana, una plantilla de volumen. Cada entreno, una sesión.' },
  { title: 'Tú decides cada semana', text: 'Tocaba MRV, pero llegas fundido: eliges la plantilla MAV y el volumen baja.' },
  { title: 'Plan contra realidad', text: 'Lo que programaste frente a lo que levantaste.' },
  { title: 'Así se monta tu entrenamiento', text: 'Empieza por el programa: lo demás se apoya en él.' },
];

const DESIGN: NodeId[] = ['prog', 'tpl', 'ses', 'sets'];
const EXECUTION: NodeId[] = ['macro', 'meso', 'week', 'train', 'done'];
/** Row of the diagram of each node: the design and execution nodes of a row are a pair */
const ROW: Record<NodeId, number> = { macro: 2, prog: 3, meso: 3, tpl: 4, week: 4, ses: 5, train: 5, sets: 6, done: 6 };

/** Index of the "you decide every week" step and of the week that is lowered in it */
const DECIDE_STEP = 3;
const ADJUSTED_WEEK = 3;

/**
 * Animated map of how training is organised in LightWeight: what is designed (program, templates per volume level,
 * sessions, sets) and what is executed with it (macrocycle, mesocycle, weeks, workouts), and how each level of the
 * execution follows one of the design. Example data only: it explains, it does not show the user's training
 */
@Component({
  selector: 'app-training-map',
  standalone: true,
  templateUrl: './training-map.html',
  styleUrl: './training-map.css',
})
export class TrainingMap {
  /** Text of the button of the last step */
  ctaLabel = input('Diseñar mi primer programa');
  /** The button of the last step was pressed */
  start = output<void>();

  readonly steps = STEPS;
  readonly levelLabel: Record<Level, string> = { mv: 'MV', mev: 'MEV', mav: 'MAV', mrv: 'MRV' };

  // ---------- example data ----------
  readonly templates: { level: Level; name: string; sets: number }[] = [
    { level: 'mev', name: 'Base', sets: 10 },
    { level: 'mav', name: 'Acumulación', sets: 14 },
    { level: 'mrv', name: 'Pico', sets: 18 },
    { level: 'mv', name: 'Descarga', sets: 6 },
  ];
  readonly plannedWeeks: Level[] = ['mev', 'mav', 'mav', 'mrv', 'mrv', 'mv'];
  readonly currentWeek = 3;
  readonly sessions = ['Push A', 'Pull A', 'Legs A'];
  readonly plannedSets = [
    { exercise: 'Press banca', scheme: '3 × 8–10', effort: 'RIR 2' },
    { exercise: 'Press inclinado', scheme: '3 × 10–12', effort: 'RIR 1' },
  ];
  readonly workouts = [
    { day: 'Lun', session: 'Push A', state: 'ok' },
    { day: 'Mié', session: 'Pull A', state: 'ok' },
    { day: 'Vie', session: 'Legs A', state: 'next' },
  ];
  readonly performedSets = [
    { exercise: 'Press banca', load: '80 kg', reps: '10 · 9 · 8' },
    { exercise: 'Press inclinado', load: '30 kg', reps: '12 · 11 · 10' },
  ];

  // ---------- state ----------
  step = signal(0);
  /** Week 4 has already been lowered from MRV to MAV in the "you decide" step */
  private swapped = signal(false);
  /** Row the pointer is over on the last step, to light up the pair */
  hoverRow = signal<number | null>(null);

  current = computed(() => STEPS[this.step()]);
  isLast = computed(() => this.step() === STEPS.length - 1);
  isDeciding = computed(() => this.step() === DECIDE_STEP);

  weeks = computed(() => {
    const lowered = this.step() > DECIDE_STEP || (this.isDeciding() && this.swapped());
    return this.plannedWeeks.map((level, i) => ({
      number: i + 1,
      level: lowered && i === ADJUSTED_WEEK ? 'mav' as Level : level,
      adjusted: lowered && i === ADJUSTED_WEEK,
      current: i === this.currentWeek,
    }));
  });

  private timer: ReturnType<typeof setInterval> | null = null;
  private swapTimer: ReturnType<typeof setTimeout> | null = null;
  private reducedMotion = typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this.stopAutoplay();
      if (this.swapTimer) clearTimeout(this.swapTimer);
    });
    this.play();
  }

  /** Classes of a node for the current step (hidden, dimmed, pulsing or highlighted) */
  nodeState(id: NodeId): string {
    const step = this.step();
    const classes: string[] = [];
    if (step === 0 && EXECUTION.includes(id)) classes.push('off');
    if (step === 2 && id !== 'macro') classes.push('pulse');
    if (step === DECIDE_STEP) classes.push(id === 'tpl' || id === 'week' ? 'hl' : 'dim');
    if (step === 4) classes.push(id === 'sets' || id === 'done' ? 'hl' : 'dim');
    if (this.isLast() && this.hoverRow() === ROW[id]) classes.push('hl');
    return classes.join(' ');
  }

  linkState(row: number): string {
    const step = this.step();
    if (step < 2) return '';
    const dim = (step === DECIDE_STEP && row !== 4) || (step === 4 && row !== 6);
    return dim ? 'on dim' : 'on';
  }

  frameHidden = computed(() => this.step() === 0);

  /** Stagger of the reveal and of the pulse, row by row */
  delay(id: NodeId): string {
    return `${(ROW[id] - 3) * 140}ms`;
  }

  pulseDelay(id: NodeId): string {
    return `${(ROW[id] - 3) * 380}ms`;
  }

  linkDelay(row: number): string {
    return `${(row - 3) * 380}ms`;
  }

  // ---------- navigation ----------
  go(index: number) {
    this.step.set(Math.max(0, Math.min(STEPS.length - 1, index)));
    this.swapped.set(false);
    if (this.swapTimer) clearTimeout(this.swapTimer);
    if (this.isDeciding()) {
      // A beat after the step appears, week 4 drops from MRV to MAV
      this.swapTimer = setTimeout(() => this.swapped.set(true), this.reducedMotion ? 0 : 1400);
    }
  }

  next() {
    this.stopAutoplay();
    if (this.isLast()) this.play();
    else this.go(this.step() + 1);
  }

  previous() {
    this.stopAutoplay();
    this.go(this.step() - 1);
  }

  select(index: number) {
    this.stopAutoplay();
    this.go(index);
  }

  /** Plays the steps from the start, one every few seconds, and stops at the last one */
  play() {
    this.stopAutoplay();
    if (this.reducedMotion) {
      this.go(STEPS.length - 1);
      return;
    }
    this.go(0);
    this.timer = setInterval(() => {
      if (this.isLast()) this.stopAutoplay();
      else this.go(this.step() + 1);
    }, 3600);
  }

  private stopAutoplay() {
    if (this.timer) clearInterval(this.timer);
    this.timer = null;
  }
}
