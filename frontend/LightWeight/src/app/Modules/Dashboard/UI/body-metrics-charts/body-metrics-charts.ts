import { Component, computed, inject, OnInit, signal, Signal, WritableSignal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { provideCharts } from 'ng2-charts';
import { Legend, LinearScale, LineController, LineElement, PointElement, Tooltip } from 'chart.js';
import { BodyMetricsChartsStore } from '../../state/body-metrics-charts.store';
import { EvolutionPoint } from '../../data/dashboard-api.service';
import { ChartSeries, LineChart } from '../line-chart/line-chart';
import { MAX_SERIES, SERIES_COLORS } from '../chart-theme';
import { MeasureField, PERIMETERS_CONFIG, SKINFOLDS_CONFIG } from '../../../BodyMetrics/UI/measurement-config';

/** A field shown in a chart and the color slot it keeps while it stays selected */
interface SelectedField {
    key: string;
    slot: number;
}

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

    private perimeterSelection = signal<SelectedField[]>([]);
    perimeterFields = computed(() => this.fieldsWithData(PERIMETERS_CONFIG.fields, this.store.perimeters()));
    selectedPerimeters = computed(() => this.effectiveSelection(this.perimeterSelection(), this.perimeterFields(), 'waist'));
    perimeterSeries = computed(() => this.seriesOf(this.selectedPerimeters(), PERIMETERS_CONFIG.fields, this.store.perimeters()));

    togglePerimeter(key: string) {
        this.toggle(this.perimeterSelection, this.selectedPerimeters, key);
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

    private skinFoldSelection = signal<SelectedField[]>([]);
    skinFoldFields = computed(() => this.fieldsWithData(SKINFOLDS_CONFIG.fields, this.store.skinFolds()));
    selectedSkinFolds = computed(() => this.effectiveSelection(this.skinFoldSelection(), this.skinFoldFields(), 'abdominal'));
    skinFoldSeries = computed(() => this.seriesOf(this.selectedSkinFolds(), SKINFOLDS_CONFIG.fields, this.store.skinFolds()));

    toggleSkinFold(key: string) {
        this.toggle(this.skinFoldSelection, this.selectedSkinFolds, key);
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

    /** The user's selection limited to fields with data; when nothing is left, the default (or the first one) */
    private effectiveSelection(selection: SelectedField[], available: MeasureField[], defaultKey: string): SelectedField[] {
        const valid = selection.filter(s => available.some(f => f.key === s.key));
        if (valid.length > 0 || available.length === 0) return valid;
        const first = available.find(f => f.key === defaultKey) ?? available[0];
        return [{ key: first.key, slot: 0 }];
    }

    /**
     * A new field takes the lowest free color and keeps it while selected: unselecting another one
     * never repaints the rest. No more than MAX_SERIES at once
     */
    private toggle(selection: WritableSignal<SelectedField[]>, current: Signal<SelectedField[]>, key: string) {
        const fields = current();
        if (fields.some(s => s.key === key)) {
            if (fields.length > 1) selection.set(fields.filter(s => s.key !== key));
            return;
        }
        if (fields.length >= MAX_SERIES) return;
        const slot = [...Array(MAX_SERIES).keys()].find(i => !fields.some(s => s.slot === i))!;
        selection.set([...fields, { key, slot }]);
    }

    private seriesOf(selection: SelectedField[], fields: MeasureField[], points: EvolutionPoint[]): ChartSeries[] {
        return selection.map(s => ({
            label: fields.find(f => f.key === s.key)?.label ?? s.key,
            color: SERIES_COLORS[s.slot],
            points: points.map(p => ({ x: Date.parse(p.measuredAt), y: p[s.key] as number | null })),
        }));
    }
}
