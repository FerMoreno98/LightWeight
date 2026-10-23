import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { provideCharts } from 'ng2-charts';
import { Legend, LinearScale, LineController, LineElement, PointElement, Tooltip } from 'chart.js';
import { ExerciseProgressStore } from '../../state/exercise-progress.store';
import { ChartSeries, LineChart } from '../line-chart/line-chart';
import { SERIES_COLORS } from '../chart-theme';
import { techniqueLabel, wellbeingLine, ZONE_LABELS, ZONE_ORDER } from '../training-labels';
import { RepZone } from '../../data/dashboard-api.service';

type FatigueMeasure = 'firstToSecond' | 'firstToLast';

const dayMonth = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'short' });
const fullDate = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'short', year: 'numeric' });
const number = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 2 });

/** The whole history of one exercise: estimated 1RM and best weight, fatigue between sets by zone, and its last sessions */
@Component({
    selector: 'app-exercise-progress',
    standalone: true,
    imports: [LineChart, RouterLink],
    templateUrl: './exercise-progress.html',
    // Chart.js is only downloaded with the components that draw charts
    providers: [provideCharts({ registerables: [LineController, LineElement, PointElement, LinearScale, Tooltip, Legend] })],
})
export class ExerciseProgressPage implements OnInit {
    store = inject(ExerciseProgressStore);
    private route = inject(ActivatedRoute);
    private destroyRef = inject(DestroyRef);

    readonly zoneLabels = ZONE_LABELS;
    readonly techniqueLabel = techniqueLabel;
    fatigueMeasure = signal<FatigueMeasure>('firstToSecond');

    // ---------------------------------------------------------------- Estimated 1RM and best weight

    progressionSeries = computed<ChartSeries[]>(() => {
        const progression = this.store.detail()?.progression ?? [];
        const point = (p: (typeof progression)[number], y: number | null, hollow = false) => ({
            x: Date.parse(p.performedAt),
            y,
            hollow,
            title: `${p.sessionName} · ${dayMonth.format(new Date(p.performedAt))}`,
            note: hollow ? 'estimación menos precisa' : undefined,
            details: [`${ZONE_LABELS[p.zone]} · ${wellbeingLine(p.sleep, p.doms, p.motivation)}`],
        });
        const series: ChartSeries[] = [];
        if (progression.some(p => p.estimatedOneRepMax !== null))
            series.push({ label: '1RM estimado', color: SERIES_COLORS[0], points: progression.map(p => point(p, p.estimatedOneRepMax, p.lowPrecision)) });
        if (progression.some(p => p.bestWeight !== null))
            series.push({ label: 'Mejor peso', color: SERIES_COLORS[1], points: progression.map(p => point(p, p.bestWeight)) });
        return series;
    });

    hasLowPrecision = computed(() => this.store.detail()?.progression.some(p => p.lowPrecision) ?? false);

    // ---------------------------------------------------------------- Fatigue between sets, by zone

    fatigueSeries = computed<ChartSeries[]>(() => {
        const fatigue = this.store.detail()?.fatigue ?? [];
        const measure = this.fatigueMeasure();
        // Each zone always takes the same color, whatever zones the exercise has
        return ZONE_ORDER
            .map((zone, slot) => ({ zone, slot, points: fatigue.filter(f => f.zone === zone) }))
            .filter(z => z.points.length > 0)
            .map(z => ({
                label: ZONE_LABELS[z.zone],
                color: SERIES_COLORS[z.slot],
                points: z.points.map(f => ({ x: Date.parse(f.performedAt), y: f[measure] })),
            }));
    });

    // ---------------------------------------------------------------- Common

    ngOnInit() {
        // The same component is reused when navigating from one exercise to another
        this.route.paramMap
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(params => {
                const exerciseId = params.get('exerciseId');
                if (exerciseId) this.store.LoadExercise(exerciseId);
            });
    }

    date(value: string): string {
        return fullDate.format(new Date(value));
    }

    zone(zone: RepZone): string {
        return ZONE_LABELS[zone];
    }

    /** "100 kg × 8 @ 8", or "PC × 12 @ 9" for body weight */
    formatSet(set: { weight: number; repetitions: number; rpe: number; isBodyWeight: boolean }): string {
        const load = set.isBodyWeight ? 'PC' : `${number.format(set.weight)} kg`;
        return `${load} × ${set.repetitions} @ ${number.format(set.rpe)}`;
    }
}
