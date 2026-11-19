import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { provideCharts } from 'ng2-charts';
import { BarController, BarElement, Legend, LinearScale, LineController, LineElement, PointElement, Tooltip } from 'chart.js';
import { TrainingOverviewStore } from '../../state/training-overview.store';
import { ScopeToggle } from '../scope-toggle/scope-toggle';
import { SessionPerformance } from '../../data/dashboard-api.service';
import { ChartSeries, LineChart } from '../line-chart/line-chart';
import { BarChart } from '../bar-chart/bar-chart';
import { ChartPoint, formatValue } from '../time-chart-options';
import { CHART_DEFAULTS, MAX_SERIES, SERIES_COLORS, withAlpha } from '../chart-theme';
import { CHART_PLUGINS } from '../chart-plugins';
import { StackedBarChart } from '../stacked-bar-chart/stacked-bar-chart';
import { SeriesSelection } from '../series-selection';
import { MUSCLE_ORDER, muscleLabel, wellbeingLine } from '../training-labels';

const dayMonth = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'short' });
const kcal = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 0 });
const perKg = new Intl.NumberFormat('es-ES', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

/** What is shown under the performance: how the user arrived (wellbeing) or what they ate (FatSecret diary) */
export type SessionContext = 'wellbeing' | 'nutrition';

/** A thin row under the performance, on the same dates */
export interface ContextRow {
    series: ChartSeries[];
    /** Right of the title: the scale or the reference of the row */
    scale: string;
    yRange: { min: number; max: number };
    band?: { min: number; max: number; color?: string };
}

/** Kcal and macros: the fourth slot of the palette and two colors that do not compete with the wellbeing */
const NUTRITION_COLORS = { calories: SERIES_COLORS[3], carbohydrate: '#a98ae8', protein: '#5fb3d9' } as const;
/** Orientative range of daily protein, g per kg of body weight */
const PROTEIN_BAND = { min: 1.6, max: 2.2 };
/**
 * Below this, the Nutrición rows are replaced by a message: one point says nothing. Sessions with diary the day
 * before, and different diary days among them (two sessions of the same day use the same day of diary)
 */
export const MIN_SESSIONS_WITH_DIARY = 2;
export const MIN_DIARY_DAYS = 2;
/** Calendar day of a session in Madrid, the same zone as the diary days */
const madridDay = new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Madrid', year: 'numeric', month: '2-digit', day: '2-digit' });

/**
 * Overview of the training in the home: weekly volume per muscle group, and the performance of each session
 * stacked over the wellbeing it started with, to see at a glance why a session went better or worse
 */
@Component({
    selector: 'app-training-overview',
    standalone: true,
    imports: [LineChart, BarChart, StackedBarChart, ScopeToggle],
    templateUrl: './training-overview.html',
    // Chart.js is only downloaded with the components that draw charts
    providers: [provideCharts({ registerables: [LineController, LineElement, BarController, BarElement, PointElement, LinearScale, Tooltip, Legend, ...CHART_PLUGINS], defaults: CHART_DEFAULTS })],
})
export class TrainingOverview implements OnInit {
    store = inject(TrainingOverviewStore);
    readonly maxSeries = MAX_SERIES;
    readonly muscleLabel = muscleLabel;

    // ---------------------------------------------------------------- Weekly volume

    /** Muscle groups trained at least once, in the order of training */
    muscles = computed(() => {
        const trained = new Set(this.store.volume().flatMap(w => Object.keys(w.setsByMuscle)));
        return [...trained].sort((a, b) => MUSCLE_ORDER.indexOf(a.toLowerCase()) - MUSCLE_ORDER.indexOf(b.toLowerCase()));
    });

    /** By default, the muscle group with the most sets */
    private mostTrained = computed(() => {
        const totals = new Map<string, number>();
        for (const week of this.store.volume())
            for (const [muscle, sets] of Object.entries(week.setsByMuscle)) totals.set(muscle, (totals.get(muscle) ?? 0) + sets);
        return [...totals.entries()].sort((a, b) => b[1] - a[1])[0]?.[0];
    });

    muscleSelection = new SeriesSelection(this.muscles, () => this.mostTrained());

    volumeSeries = computed<ChartSeries[]>(() =>
        this.muscleSelection.selected().map(s => ({
            label: muscleLabel(s.key),
            color: SERIES_COLORS[s.slot],
            points: this.store.volume().map(w => ({
                x: Date.parse(w.startedAt),
                y: w.setsByMuscle[s.key] ?? 0,
                title: `Semana ${w.weekNumber} · ${dayMonth.format(new Date(w.startedAt))}`,
            })),
        })),
    );

    // ---------------------------------------------------------------- Session performance and wellbeing

    /** Every session date: the two stacked charts share it, so each session falls on the same vertical */
    sessionDates = computed(() => this.store.sessions().map(s => Date.parse(s.startedAt)));

    hasPerformance = computed(() => this.store.sessions().some(s => s.performance !== null));

    performancePoints = computed<ChartPoint[]>(() =>
        this.store.sessions()
            .filter(s => s.performance !== null)
            .map(s => ({
                x: Date.parse(s.startedAt),
                y: s.performance,
                title: this.sessionTitle(s.name, s.startedAt),
                note: `${s.comparedExercises} ${s.comparedExercises === 1 ? 'ejercicio' : 'ejercicios'}`,
                details: [wellbeingLine(s.sleep, s.doms, s.motivation), ...this.nutritionLines(s), ...this.excludedLines(s)],
            })),
    );

    wellbeingSeries = computed<ChartSeries[]>(() => {
        const sessions = this.store.sessions();
        const series = (label: string, color: string, value: (s: (typeof sessions)[number]) => number): ChartSeries => ({
            label,
            color,
            area: true,
            quietPoints: true,
            endLabel: true,
            points: sessions.map(s => ({
                x: Date.parse(s.startedAt),
                y: value(s),
                title: this.sessionTitle(s.name, s.startedAt),
                details: [
                    ...(s.performance === null ? [] : [`rendimiento ${formatValue(s.performance, '%', true)}`]),
                    ...this.excludedLines(s),
                ],
            })),
        });
        return [
            series('Sueño', SERIES_COLORS[0], s => s.sleep),
            series('Agujetas', SERIES_COLORS[1], s => s.doms),
            series('Motivación', SERIES_COLORS[2], s => s.motivation),
        ];
    });

    /** One row per measure: each one is read alone and they are compared looking down the same date */
    wellbeingRows = computed<ContextRow[]>(() => this.wellbeingSeries().map(s => ({ series: [s], scale: '1 – 10', yRange: { min: 1, max: 10 } })));

    // ---------------------------------------------------------------- What the user ate (FatSecret)

    /** Some session has diary around it: the Nutrición tab is offered */
    hasNutrition = computed(() => this.store.sessions().some(s => s.nutrition !== null));

    private _context = signal<SessionContext>('wellbeing');
    /** Without diary there is only the wellbeing */
    context = computed<SessionContext>(() => (this.hasNutrition() ? this._context() : 'wellbeing'));

    selectContext(context: SessionContext) {
        this._context.set(context);
    }

    /** Sessions of the chart with diary the day before */
    private sessionsWithDiary = computed(() => this.store.sessions().filter(s => s.nutrition?.previousDayCalories != null));

    /** Enough to compare: two sessions with diary the day before, on two different days of diary */
    nutritionReady = computed(() => {
        const sessions = this.sessionsWithDiary();
        const days = new Set(sessions.map(s => madridDay.format(new Date(s.startedAt))));
        return sessions.length >= MIN_SESSIONS_WITH_DIARY && days.size >= MIN_DIARY_DAYS;
    });

    nutritionRows = computed<ContextRow[]>(() => {
        const sessions = this.store.sessions();
        const values = (pick: (n: NonNullable<SessionPerformance['nutrition']>) => number | null) =>
            sessions.map(s => (s.nutrition ? pick(s.nutrition) : null));
        const series = (label: string, color: string, ys: (number | null)[], details: (s: SessionPerformance) => string[]): ChartSeries => ({
            label,
            color,
            area: true,
            quietPoints: true,
            endLabel: true,
            points: sessions.map((s, i) => ({
                x: Date.parse(s.startedAt),
                y: ys[i],
                title: this.sessionTitle(s.name, s.startedAt),
                details: [...(s.performance === null ? [] : [`rendimiento ${formatValue(s.performance, '%', true)}`]), ...details(s)],
            })),
        });
        const present = (ys: (number | null)[]) => ys.filter((v): v is number => v !== null);

        const calories = values(n => n.previousDayCalories);
        const knownCalories = present(calories);
        const average = knownCalories.length ? knownCalories.reduce((a, b) => a + b, 0) / knownCalories.length : null;
        const carbohydrate = values(n => n.previousDayCarbohydratePerKg);
        const protein = values(n => n.proteinPerKgLast7Days);
        return [
            {
                series: [series('Kcal · día anterior', NUTRITION_COLORS.calories, calories, s =>
                    s.nutrition?.sameDayCalories != null ? [`mismo día ${kcal.format(s.nutrition.sameDayCalories)} kcal`] : [])],
                scale: average === null ? 'kcal' : `tu media ${kcal.format(average)}`,
                yRange: {
                    min: Math.max(0, Math.floor((Math.min(...knownCalories, 2000) - 200) / 100) * 100),
                    max: Math.ceil((Math.max(...knownCalories, 2000) + 200) / 100) * 100,
                },
            },
            {
                series: [series('Carbohidratos · día anterior', NUTRITION_COLORS.carbohydrate, carbohydrate, () => [])],
                scale: 'g/kg',
                yRange: { min: 0, max: Math.max(6, Math.ceil(Math.max(0, ...present(carbohydrate)))) },
            },
            {
                series: [series('Proteína · 7 días', NUTRITION_COLORS.protein, protein, () => [])],
                scale: 'g/kg',
                yRange: { min: 1, max: Math.max(3, Math.ceil(Math.max(0, ...present(protein)) * 2) / 2) },
                band: { ...PROTEIN_BAND, color: withAlpha(NUTRITION_COLORS.protein, 0.12) },
            },
        ];
    });

    /** The rows of the selected tab */
    contextRows = computed<ContextRow[]>(() => (this.context() === 'nutrition' ? this.nutritionRows() : this.wellbeingRows()));

    // ---------------------------------------------------------------- Common

    ngOnInit() {
        // Always reload: picks up the session just finished
        this.store.Load();
    }

    rebuild() {
        this.store.Rebuild();
    }

    /**
     * Exercises left out of the session's average because their change is not plausible (over ±50 %): they are
     * shown, never hidden, so the user can check them (usually a registration error or a weighted exercise)
     */
    private excludedLines(session: SessionPerformance): string[] {
        return session.excluded.map(e => `sin contar ${e.exerciseName} (${formatValue(e.change, '%', true)}): ¿error al registrar?`);
    }

    /** Lines of the performance tooltip with what the user ate around the session */
    private nutritionLines(session: SessionPerformance): string[] {
        const n = session.nutrition;
        if (!n) return [];
        const lines: string[] = [];
        if (n.previousDayCalories !== null) {
            const carbs = n.previousDayCarbohydratePerKg !== null ? ` · ${perKg.format(n.previousDayCarbohydratePerKg)} g/kg de carbos` : '';
            lines.push(`día anterior ${kcal.format(n.previousDayCalories)} kcal${carbs}`);
        }
        if (n.sameDayCalories !== null) lines.push(`mismo día ${kcal.format(n.sameDayCalories)} kcal`);
        if (n.proteinPerKgLast7Days !== null) lines.push(`proteína 7 días ${perKg.format(n.proteinPerKgLast7Days)} g/kg`);
        return lines;
    }

    private sessionTitle(name: string, startedAt: string): string {
        return `${name} · ${dayMonth.format(new Date(startedAt))}`;
    }
}
