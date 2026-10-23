import { Component, computed, inject, OnInit } from '@angular/core';
import { provideCharts } from 'ng2-charts';
import { BarController, BarElement, Legend, LinearScale, LineController, LineElement, PointElement, Tooltip } from 'chart.js';
import { TrainingOverviewStore } from '../../state/training-overview.store';
import { SessionPerformance } from '../../data/dashboard-api.service';
import { ChartSeries, LineChart } from '../line-chart/line-chart';
import { BarChart } from '../bar-chart/bar-chart';
import { ChartPoint, formatValue } from '../time-chart-options';
import { MAX_SERIES, SERIES_COLORS } from '../chart-theme';
import { SeriesSelection } from '../series-selection';
import { MUSCLE_ORDER, muscleLabel, wellbeingLine } from '../training-labels';

const dayMonth = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'short' });

/**
 * Overview of the training in the home: weekly volume per muscle group, and the performance of each session
 * stacked over the wellbeing it started with, to see at a glance why a session went better or worse
 */
@Component({
    selector: 'app-training-overview',
    standalone: true,
    imports: [LineChart, BarChart],
    templateUrl: './training-overview.html',
    // Chart.js is only downloaded with the components that draw charts
    providers: [provideCharts({ registerables: [LineController, LineElement, BarController, BarElement, PointElement, LinearScale, Tooltip, Legend] })],
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
                details: [wellbeingLine(s.sleep, s.doms, s.motivation), ...this.excludedLines(s)],
            })),
    );

    wellbeingSeries = computed<ChartSeries[]>(() => {
        const sessions = this.store.sessions();
        const series = (label: string, color: string, value: (s: (typeof sessions)[number]) => number): ChartSeries => ({
            label,
            color,
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

    private sessionTitle(name: string, startedAt: string): string {
        return `${name} · ${dayMonth.format(new Date(startedAt))}`;
    }
}
