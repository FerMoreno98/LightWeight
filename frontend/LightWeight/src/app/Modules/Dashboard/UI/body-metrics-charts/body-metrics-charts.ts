import { Component, computed, inject, OnInit } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { provideCharts } from 'ng2-charts';
import { Legend, LinearScale, LineController, LineElement, PointElement, Tooltip } from 'chart.js';
import { BodyMetricsChartsStore } from '../../state/body-metrics-charts.store';
import { EvolutionPoint } from '../../data/dashboard-api.service';
import { ChartSeries, LineChart } from '../line-chart/line-chart';
import { MAX_SERIES, SERIES_COLORS } from '../chart-theme';
import { SelectedField, SeriesSelection } from '../series-selection';
import { MeasureField, PERIMETERS_CONFIG, SKINFOLDS_CONFIG } from '../../../BodyMetrics/UI/measurement-config';

const HALF_WEEK_MS = 3.5 * 24 * 60 * 60 * 1000;
const dayMonth = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'short' });
const number = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 2 });

/**
 * Evolution of the body measurements, read from the dashboard: weight with its weekly average,
 * perimeters, summatory of skinfolds and each skinfold. Shown inside the BodyMetrics page
 */
@Component({
    selector: 'app-body-metrics-charts',
    standalone: true,
    imports: [LineChart, NgTemplateOutlet],
    templateUrl: './body-metrics-charts.html',
    // Registered here and not in app.config: Chart.js is only downloaded when the charts are opened.
    // Only the pieces the line charts use, to keep the chunk small
    providers: [provideCharts({ registerables: [LineController, LineElement, PointElement, LinearScale, Tooltip, Legend] })],
})
export class BodyMetricsCharts implements OnInit {
    store = inject(BodyMetricsChartsStore);

    readonly perimeterUnit = PERIMETERS_CONFIG.unit;
    readonly skinFoldUnit = SKINFOLDS_CONFIG.unit;
    readonly maxSeries = MAX_SERIES;

    // ---------------------------------------------------------------- Weight

    weightSeries = computed<ChartSeries[]>(() => {
        const { points, weeklyAverages } = this.store.weight();
        return [
            {
                label: 'Pesajes',
                color: SERIES_COLORS[0],
                // Thin: the daily ups and downs stay visible but the weekly average stands out
                lineWidth: 1,
                points: points.map(p => ({ x: Date.parse(p.measuredAt), y: p.weight })),
            },
            {
                label: 'Media semanal',
                color: SERIES_COLORS[1],
                lineWidth: 3,
                // Drawn in the middle of its week, where it belongs
                points: weeklyAverages.map(a => ({
                    x: Date.parse(a.weekStart) + HALF_WEEK_MS,
                    y: a.average,
                    note: `semana del ${dayMonth.format(new Date(a.weekStart))}, ${a.count} ${a.count === 1 ? 'pesaje' : 'pesajes'}`,
                })),
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

    // ---------------------------------------------------------------- Perimeters

    perimeterFields = computed(() => this.fieldsWithData(PERIMETERS_CONFIG.fields, this.store.perimeters()));
    private perimeterSelection = new SeriesSelection(computed(() => this.perimeterFields().map(f => f.key)), () => 'waist');
    selectedPerimeters = this.perimeterSelection.selected;
    perimeterSeries = computed(() => this.seriesOf(this.selectedPerimeters(), PERIMETERS_CONFIG.fields, this.store.perimeters()));

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
