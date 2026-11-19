import { Component, computed, input } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartDataset, ChartOptions, ScriptableContext, ScriptableScaleContext } from 'chart.js';
import { CHART_CHROME, withAlpha } from '../chart-theme';
import { ChartPoint, timeChartOptions } from '../time-chart-options';

export type { ChartPoint } from '../time-chart-options';

export interface ChartSeries {
    label: string;
    color: string;
    points: ChartPoint[];
    /** Width of the line in px (2 by default): a thinner one recedes behind a highlighted series */
    lineWidth?: number;
    /** 'dots': only small faint points, no line, as a background for another series (e.g. each weigh-in) */
    style?: 'line' | 'dots';
    /** Gradient area under the line, for the protagonist of the chart */
    area?: boolean;
    /** Points only on hover: a cleaner line. Hollow points stay visible, they carry information */
    quietPoints?: boolean;
    /** Last value written next to the end of the line */
    endLabel?: boolean;
}

/**
 * 'default' and 'compact' differ in height; 'spark' is a thin row with no axes, for several measures stacked one
 * per row on the same dates (it needs a fixed yRange)
 */
export type LineChartVariant = 'default' | 'compact' | 'spark';

const HEIGHTS: Record<LineChartVariant, string> = {
    default: 'h-64 sm:h-72',
    compact: 'h-44 sm:h-48',
    spark: 'h-20 sm:h-24',
};

/**
 * Line chart over time, themed for the app. The X axis is linear over timestamps, so the distance
 * between points is the real time between measurements (no date adapter needed)
 */
@Component({
    selector: 'app-line-chart',
    standalone: true,
    imports: [BaseChartDirective],
    template: `
        <div class="relative" [class]="height()">
            <canvas baseChart type="line" [data]="data()" [options]="options()" [attr.aria-label]="ariaLabel()" role="img"></canvas>
        </div>
    `,
})
export class LineChart {
    series = input.required<ChartSeries[]>();
    /** Unit of the values, shown in the axis and the tooltip */
    unit = input.required<string>();
    /** Description of the chart for screen readers */
    ariaLabel = input.required<string>();
    /** Timestamps the X axis must cover. Two stacked charts that receive the same list share their dates */
    xDomain = input<number[] | undefined>(undefined);
    /** Fixed Y range (e.g. 1-10); otherwise it follows the data */
    yRange = input<{ min: number; max: number } | undefined>(undefined);
    /** Values shown with their sign ("+2,4 %") */
    signed = input(false);
    variant = input<LineChartVariant>('default');
    /** Orientative range drawn as a band behind the lines (e.g. 1,6-2,2 g/kg of protein) */
    band = input<{ min: number; max: number; color?: string } | undefined>(undefined);

    height = computed(() => HEIGHTS[this.variant()]);

    data = computed<ChartData<'line', ChartPoint[]>>(() => ({
        datasets: this.series().map(s => (s.style === 'dots' ? this.dots(s) : this.line(s))),
    }));

    options = computed(() => {
        const series = this.series();
        const options = timeChartOptions({
            xs: this.xDomain() ?? series.flatMap(s => s.points.map(p => p.x)),
            unit: this.unit(),
            signed: this.signed(),
            yRange: this.yRange(),
            showLegend: series.length > 1,
            endLabels: series.flatMap((s, i) => (s.endLabel ? [i] : [])),
        }) as ChartOptions<'line'>;
        if (this.variant() === 'spark') this.toSpark(options);
        const band = this.band();
        if (band) options.plugins!.referenceBand = band;
        return options;
    });

    private line(s: ChartSeries): ChartDataset<'line', ChartPoint[]> {
        const hollow = (ctx: ScriptableContext<'line'>) => (ctx.raw as ChartPoint | undefined)?.hollow === true;
        return {
            label: s.label,
            data: s.points,
            borderColor: s.color,
            backgroundColor: s.area ? gradientUnder(s.color) : s.color,
            fill: s.area ? 'start' : false,
            borderWidth: s.lineWidth ?? 2,
            spanGaps: true,
            tension: s.quietPoints ? 0.35 : 0.25,
            pointRadius: s.quietPoints ? ctx => (hollow(ctx) ? 4 : 0) : 4,
            pointHoverRadius: 6,
            // A hollow point is an empty ring in the color of its series
            pointBackgroundColor: ctx => (hollow(ctx) ? CHART_CHROME.surface : s.color),
            pointBorderColor: ctx => (hollow(ctx) ? s.color : CHART_CHROME.surface),
            pointBorderWidth: 2,
        };
    }

    private dots(s: ChartSeries): ChartDataset<'line', ChartPoint[]> {
        return {
            label: s.label,
            data: s.points,
            showLine: false,
            borderColor: s.color,
            backgroundColor: s.color,
            pointRadius: 2.5,
            pointHoverRadius: 5,
            pointBackgroundColor: withAlpha(s.color, 0.5),
            pointHoverBackgroundColor: s.color,
            pointBorderWidth: 0,
        };
    }

    /** No axes and no legend: the row is named by its title and the dates are read in the chart above */
    private toSpark(options: ChartOptions<'line'>) {
        const scales = options.scales!;
        scales['x']!.display = false;
        const range = this.yRange();
        const middle = range ? Math.round((range.min + range.max) / 2) : null;
        Object.assign(scales['y']!, {
            ticks: { ...scales['y']!.ticks, display: false },
            // Only a faint line in the middle of the range, as a reference
            grid: { color: (ctx: ScriptableScaleContext) => (ctx.tick?.value === middle ? CHART_CHROME.grid : 'transparent') },
        });
        options.layout = { padding: { ...(options.layout?.padding as object), top: 6, bottom: 6 } };
        options.plugins!.legend!.display = false;
    }
}

/** Fill that fades from the color of the line to nothing at the bottom of the chart */
function gradientUnder(color: string) {
    return (ctx: ScriptableContext<'line'>) => {
        const { chart } = ctx;
        const area = chart.chartArea;
        if (!area) return 'transparent';
        const gradient = chart.ctx.createLinearGradient(0, area.top, 0, area.bottom);
        gradient.addColorStop(0, withAlpha(color, 0.3));
        gradient.addColorStop(1, withAlpha(color, 0));
        return gradient;
    };
}
