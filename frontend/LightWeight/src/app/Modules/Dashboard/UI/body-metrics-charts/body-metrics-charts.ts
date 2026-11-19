import { Component, computed, inject, OnInit } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { provideCharts } from 'ng2-charts';
import { BarController, BarElement, CategoryScale, Legend, LinearScale, LineController, LineElement, PointElement, Tooltip } from 'chart.js';
import { BodyMetricsChartsStore } from '../../state/body-metrics-charts.store';
import { EvolutionPoint } from '../../data/dashboard-api.service';
import { ChartSeries, LineChart } from '../line-chart/line-chart';
import { DeviationBarChart } from '../deviation-bar-chart/deviation-bar-chart';
import { ChartPoint } from '../time-chart-options';
import { CHART_CHROME, CHART_DEFAULTS, MAX_SERIES, SERIES_COLORS, withAlpha } from '../chart-theme';
import { CHART_PLUGINS } from '../chart-plugins';
import { ChangeBars, ChangeItem } from '../change-bars/change-bars';
import { SelectedField, SeriesSelection } from '../series-selection';
import { MeasureField, PERIMETERS_CONFIG, SKINFOLDS_CONFIG } from '../../../BodyMetrics/UI/measurement-config';

const HALF_WEEK_MS = 3.5 * 24 * 60 * 60 * 1000;
const dayMonth = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'short' });
const number = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 2 });
const kcal = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 0 });
const rate = new Intl.NumberFormat('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2, signDisplay: 'exceptZero' });
const WEEK_MS = 7 * 24 * 60 * 60 * 1000;

/** Color of the kcal: the fourth slot of the palette, free in the weight card */
export const CALORIES_COLOR = SERIES_COLORS[3];
/** Protein: a fixed color of its own card */
export const PROTEIN_COLOR = SERIES_COLORS[0];
/** Opacity of a week's bar by its days with diary: a week with few days is read with care */
export const DAYS_STRENGTH: { days: string; strength: number }[] = [
    { days: '7', strength: 1 },
    { days: '6', strength: 0.72 },
    { days: '5', strength: 0.48 },
    { days: '≤ 4', strength: 0.26 },
];
function strengthOf(daysLogged: number): number {
    return daysLogged >= 7 ? 1 : daysLogged === 6 ? 0.72 : daysLogged === 5 ? 0.48 : 0.26;
}

/**
 * Evolution of the body measurements, read from the dashboard: weight with its weekly average,
 * perimeters, summatory of skinfolds and each skinfold. Shown inside the BodyMetrics page
 */
@Component({
    selector: 'app-body-metrics-charts',
    standalone: true,
    imports: [LineChart, DeviationBarChart, ChangeBars, NgTemplateOutlet, RouterLink],
    templateUrl: './body-metrics-charts.html',
    // Registered here and not in app.config: Chart.js is only downloaded when the charts are opened.
    // Only the pieces the charts use, to keep the chunk small
    providers: [
        provideCharts({
            registerables: [LineController, LineElement, PointElement, BarController, BarElement, LinearScale, CategoryScale, Tooltip, Legend, ...CHART_PLUGINS],
            defaults: CHART_DEFAULTS,
        }),
    ],
})
export class BodyMetricsCharts implements OnInit {
    store = inject(BodyMetricsChartsStore);

    readonly perimeterUnit = PERIMETERS_CONFIG.unit;
    readonly skinFoldUnit = SKINFOLDS_CONFIG.unit;
    readonly maxSeries = MAX_SERIES;
    readonly caloriesColor = CALORIES_COLOR;
    readonly proteinColor = PROTEIN_COLOR;
    readonly daysStrength = DAYS_STRENGTH;
    readonly withAlpha = withAlpha;

    // ---------------------------------------------------------------- Weight

    weightSeries = computed<ChartSeries[]>(() => {
        const { points, weeklyAverages } = this.store.weight();
        // The weekly average is the trend to read; each weigh-in stays as a faint dot behind it
        return [
            {
                label: 'Media semanal',
                color: CHART_CHROME.accent,
                lineWidth: 2.5,
                area: true,
                quietPoints: true,
                endLabel: true,
                // Drawn in the middle of its week, where it belongs
                points: weeklyAverages.map(a => ({
                    x: Date.parse(a.weekStart) + HALF_WEEK_MS,
                    y: a.average,
                    note: `semana del ${dayMonth.format(new Date(a.weekStart))}, ${a.count} ${a.count === 1 ? 'pesaje' : 'pesajes'}`,
                })),
            },
            {
                label: 'Pesajes',
                color: CHART_CHROME.muted,
                style: 'dots',
                points: points.map(p => ({ x: Date.parse(p.measuredAt), y: p.weight })),
            },
        ];
    });

    /** Last weigh-in, for the header of the card */
    lastWeight = computed(() => {
        const points = this.store.weight().points;
        if (points.length === 0) return null;
        const last = points[points.length - 1];
        return `${number.format(last.weight)} kg el ${dayMonth.format(new Date(last.measuredAt))}`;
    });

    // ---------------------------------------------------------------- Nutrition, under the weight

    /** The FatSecret diary has weeks: the weight card also shows the kcal */
    hasNutrition = computed(() => this.store.nutrition().averageCalories !== null && this.store.nutrition().weeks.length > 0);

    /** Middle of each week, like the weekly weight */
    private weekX(weekStart: string): number {
        return Date.parse(`${weekStart}T00:00:00Z`) + HALF_WEEK_MS;
    }

    /** Dates of the weight chart and of the kcal under it: both cover the same ones, so each week is on the same vertical */
    weightDomain = computed<number[] | undefined>(() => {
        if (!this.hasNutrition()) return undefined;
        return [
            ...this.weightSeries().flatMap(s => s.points.map(p => p.x)),
            ...this.store.nutrition().weeks.map(w => this.weekX(w.weekStart)),
        ];
    });

    averageCalories = computed(() => kcal.format(this.store.nutrition().averageCalories ?? 0));

    /** Each week above or below the user's average, lighter with fewer days of diary */
    caloriesPoints = computed<ChartPoint[]>(() => {
        const { averageCalories, weeks } = this.store.nutrition();
        if (averageCalories === null) return [];
        return weeks.map(w => ({
            x: this.weekX(w.weekStart),
            y: w.calories - averageCalories,
            title: `Semana del ${dayMonth.format(new Date(`${w.weekStart}T00:00:00Z`))}`,
            strength: strengthOf(w.daysLogged),
            details: [`${kcal.format(w.calories)} kcal/día`, `${w.daysLogged} de 7 días con diario`],
        }));
    });

    recentCalories = computed(() => {
        const recent = this.store.nutrition().recentCalories;
        return recent === null ? null : kcal.format(recent);
    });

    /** kg per week between the last weekly average and the one at least 3 weeks before; null without both */
    weightRate = computed(() => {
        const weeks = this.store.weight().weeklyAverages;
        if (weeks.length < 2) return null;
        const last = weeks[weeks.length - 1];
        const lastAt = Date.parse(last.weekStart);
        const earlier = [...weeks].reverse().find(w => lastAt - Date.parse(w.weekStart) >= 3 * WEEK_MS);
        if (!earlier) return null;
        const weeksBetween = (lastAt - Date.parse(earlier.weekStart)) / WEEK_MS;
        return rate.format((last.average - earlier.average) / weeksBetween);
    });

    proteinSeries = computed<ChartSeries[]>(() => [
        {
            label: 'Proteína',
            color: PROTEIN_COLOR,
            endLabel: true,
            points: this.store.nutrition().weeks
                .filter(w => w.proteinPerKg !== null)
                .map(w => ({
                    x: this.weekX(w.weekStart),
                    y: w.proteinPerKg,
                    title: `Semana del ${dayMonth.format(new Date(`${w.weekStart}T00:00:00Z`))}`,
                    note: `${w.daysLogged} de 7 días con diario`,
                })),
        },
    ]);
    hasProtein = computed(() => this.proteinSeries()[0].points.length > 0);

    // ---------------------------------------------------------------- Perimeters

    perimeterFields = computed(() => this.fieldsWithData(PERIMETERS_CONFIG.fields, this.store.perimeters()));
    private perimeterSelection = new SeriesSelection(computed(() => this.perimeterFields().map(f => f.key)), () => 'waist');
    selectedPerimeters = this.perimeterSelection.selected;
    perimeterSeries = computed(() => this.seriesOf(this.selectedPerimeters(), PERIMETERS_CONFIG.fields, this.store.perimeters()));

    /** Change of each selected perimeter from its first measurement to its last one (only with two or more) */
    perimeterChanges = computed<ChangeItem[]>(() => {
        const points = this.store.perimeters();
        return this.selectedPerimeters().flatMap(s => {
            const measured = points.filter(p => p[s.key] != null);
            if (measured.length < 2) return [];
            const first = measured[0];
            const last = measured[measured.length - 1];
            return [{
                label: PERIMETERS_CONFIG.fields.find(f => f.key === s.key)?.label ?? s.key,
                color: SERIES_COLORS[s.slot],
                from: first[s.key] as number,
                to: last[s.key] as number,
                fromDate: dayMonth.format(new Date(first.measuredAt)),
                toDate: dayMonth.format(new Date(last.measuredAt)),
            }];
        });
    });

    togglePerimeter(key: string) {
        this.perimeterSelection.toggle(key);
    }

    // ---------------------------------------------------------------- Skinfolds

    /** Only the measurements with the six skinfolds have a summatory */
    summatorySeries = computed<ChartSeries[]>(() => [
        {
            label: 'Sumatorio',
            color: SERIES_COLORS[0],
            points: this.store.skinFolds()
                .filter(p => p['summatory'] != null)
                .map(p => ({ x: Date.parse(p.measuredAt), y: p['summatory'] as number })),
        },
    ]);
    hasSummatory = computed(() => this.summatorySeries()[0].points.length > 0);

    /** Estimated by the dashboard from the summatory and the sex of the profile: only where both exist */
    fatSeries = computed<ChartSeries[]>(() => [
        {
            label: 'Grasa estimada',
            color: SERIES_COLORS[0],
            points: this.store.skinFolds()
                .filter(p => p['fatPercentage'] != null)
                .map(p => ({ x: Date.parse(p.measuredAt), y: p['fatPercentage'] as number })),
        },
    ]);
    hasFat = computed(() => this.fatSeries()[0].points.length > 0);

    /** Last estimation, for the header of the card */
    lastFat = computed(() => {
        const points = this.fatSeries()[0].points;
        if (points.length === 0) return null;
        const last = points[points.length - 1];
        return `${number.format(last.y as number)} % el ${dayMonth.format(new Date(last.x))}`;
    });

    skinFoldFields = computed(() => this.fieldsWithData(SKINFOLDS_CONFIG.fields, this.store.skinFolds()));
    private skinFoldSelection = new SeriesSelection(computed(() => this.skinFoldFields().map(f => f.key)), () => 'abdominal');
    selectedSkinFolds = this.skinFoldSelection.selected;
    skinFoldSeries = computed(() => this.seriesOf(this.selectedSkinFolds(), SKINFOLDS_CONFIG.fields, this.store.skinFolds()));

    toggleSkinFold(key: string) {
        this.skinFoldSelection.toggle(key);
    }

    // ---------------------------------------------------------------- Common

    ngOnInit() {
        // Always reload: picks up what was just registered in the other tabs
        this.store.Load();
    }

    rebuild() {
        this.store.Rebuild();
    }

    colorOf(selection: SelectedField[], key: string): string | null {
        const selected = selection.find(s => s.key === key);
        return selected ? SERIES_COLORS[selected.slot] : null;
    }

    /** Fields measured at least once: the others would draw an empty line */
    private fieldsWithData(fields: MeasureField[], points: EvolutionPoint[]): MeasureField[] {
        return fields.filter(f => points.some(p => p[f.key] != null));
    }

    private seriesOf(selection: SelectedField[], fields: MeasureField[], points: EvolutionPoint[]): ChartSeries[] {
        return selection.map(s => ({
            label: fields.find(f => f.key === s.key)?.label ?? s.key,
            color: SERIES_COLORS[s.slot],
            points: points.map(p => ({ x: Date.parse(p.measuredAt), y: p[s.key] as number | null })),
        }));
    }
}
