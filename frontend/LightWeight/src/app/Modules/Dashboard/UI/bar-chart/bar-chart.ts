import { Component, computed, input } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { Chart, ChartData, ChartOptions, Plugin, ScriptableContext, ScriptableScaleContext } from 'chart.js';
import { CHART_CHROME, DIVERGING, withAlpha } from '../chart-theme';
import { ChartPoint, END_LABEL_ROOM, timeChartOptions } from '../time-chart-options';
import { barWidthPlugin } from '../bar-width';

/** Dataset of the arrows of the values out of scale: not in the legend nor the tooltip (the bar already is) */
const OUT_OF_SCALE = 2;
/** Width of the bars in px: from the usual gap between sessions, never a hairline nor a block */
const MIN_BAR = 3;
const MAX_BAR = 10;
/** Share of the usual gap taken by a bar */
const BAR_SHARE = 0.6;

/**
 * Values over time around zero, as bars from zero: blue up (better), red down (worse), and the sign is also in the
 * labels, so the meaning never depends on the color alone. Over them, the moving average of the last values: the
 * trend, which a single bad session does not hide. Same time axis as the line chart, so both can be stacked
 */
@Component({
    selector: 'app-bar-chart',
    standalone: true,
    imports: [BaseChartDirective],
    template: `
        <div class="relative h-56 sm:h-64">
            <canvas baseChart type="bar" [data]="data()" [options]="options()" [plugins]="plugins" [attr.aria-label]="ariaLabel()" role="img"></canvas>
        </div>
    `,
})
export class BarChart {
    label = input.required<string>();
    points = input.required<ChartPoint[]>();
    unit = input.required<string>();
    ariaLabel = input.required<string>();
    /** Timestamps the X axis must cover. Two stacked charts that receive the same list share their dates */
    xDomain = input<number[] | undefined>(undefined);
    /**
     * The Y axis always covers at least ±this value (and grows with the data). Without it the axis fits the data,
     * and a +0.3 % would fill the whole height as if it were a big change
     */
    minRange = input(5);
    /**
     * Values beyond ±this are drawn at the edge, with a triangle pointing out of the chart; the tooltip keeps their
     * real value. One extreme value (e.g. +300 %) would otherwise squash every other session against zero
     */
    clipAt = input(25);
    /** Values in the moving average (each one with the previous ones) */
    trendWindow = input(4);
    /** Name of the moving average in the legend and the tooltip */
    trendLabel = input('Media de 4 sesiones');

    /** The points as drawn: the ones beyond the limit are moved to the edge and keep their real value */
    private drawn = computed<ChartPoint[]>(() => {
        const limit = this.clipAt();
        return this.points().map(p => {
            if (p.y === null || Math.abs(p.y) <= limit) return p;
            const edge = Math.sign(p.y) * limit;
            return { ...p, y: edge, value: p.y, note: p.note ? `${p.note}; fuera de escala` : 'fuera de escala' };
        });
    });

    /** Moving average of the real values (not the clipped ones), drawn inside the chart */
    private trend = computed<ChartPoint[]>(() => {
        const window = this.trendWindow();
        const limit = this.clipAt();
        const values = this.points().map(p => p.y);
        return this.points().map((p, i) => {
            const last = values.slice(Math.max(0, i - window + 1), i + 1).filter((v): v is number => v !== null);
            if (last.length === 0) return { x: p.x, y: null };
            const average = last.reduce((a, b) => a + b, 0) / last.length;
            const clipped = Math.max(-limit, Math.min(limit, average));
            return { x: p.x, y: clipped, value: clipped === average ? undefined : average };
        });
    });

    // A mixed chart (bar + line datasets): Chart.js supports it through the type of each dataset, but the typing of
    // the directive expects a single type, hence the cast below
    data = computed<ChartData<'bar', ChartPoint[]>>(() => {
        const sign = (ctx: ScriptableContext<'bar'> | ScriptableContext<'line'>) =>
            ((ctx.raw as ChartPoint | undefined)?.y ?? 0) < 0 ? DIVERGING.negative : DIVERGING.positive;
        const outOfScale = (ctx: ScriptableContext<'line'>) => (ctx.raw as ChartPoint | undefined)?.value !== undefined;
        return {
            datasets: [
                {
                    type: 'bar',
                    label: this.label(),
                    data: this.drawn(),
                    // Lighter than the average line, which is the one to read first
                    backgroundColor: (ctx: ScriptableContext<'bar'>) => withAlpha(sign(ctx), 0.75),
                    hoverBackgroundColor: sign,
                    // Set by barWidthPlugin from the usual gap between sessions
                    barThickness: MAX_BAR,
                    borderRadius: 3,
                    order: 1,
                },
                {
                    type: 'line',
                    label: this.trendLabel(),
                    data: this.trend(),
                    borderColor: CHART_CHROME.text,
                    backgroundColor: CHART_CHROME.text,
                    borderWidth: 2,
                    tension: 0.35,
                    spanGaps: true,
                    pointRadius: 0,
                    pointHoverRadius: 5,
                    pointBorderColor: CHART_CHROME.surface,
                    pointBorderWidth: 2,
                    // Drawn over the bars
                    order: 0,
                },
                {
                    // A clipped value ends in a triangle pointing out of the chart (down when negative)
                    type: 'line',
                    label: this.label(),
                    data: this.drawn(),
                    showLine: false,
                    // The triangle sits on the edge: drawn even if it sticks out of the chart area
                    clip: false,
                    pointStyle: 'triangle',
                    rotation: (ctx: ScriptableContext<'line'>) => (((ctx.raw as ChartPoint | undefined)?.y ?? 0) < 0 ? 180 : 0),
                    pointRadius: (ctx: ScriptableContext<'line'>) => (outOfScale(ctx) ? 7 : 0),
                    pointHoverRadius: (ctx: ScriptableContext<'line'>) => (outOfScale(ctx) ? 7 : 0),
                    pointBackgroundColor: sign,
                    pointBorderColor: CHART_CHROME.surface,
                    pointBorderWidth: 2,
                    order: 0,
                },
            ],
        } as unknown as ChartData<'bar', ChartPoint[]>;
    });

    readonly plugins: Plugin<'bar'>[] = [
        barWidthPlugin({
            xs: () => this.points().map(p => p.x),
            domain: () => this.xDomain(),
            rightPadding: END_LABEL_ROOM,
            min: MIN_BAR,
            max: MAX_BAR,
            share: BAR_SHARE,
        }),
    ];

    options = computed(() => {
        const options = timeChartOptions({
            xs: this.xDomain() ?? this.points().map(p => p.x),
            unit: this.unit(),
            signed: true,
            showLegend: true,
            endLabels: [1],
        }) as ChartOptions<'bar'>;
        const range = this.minRange();
        const limit = this.clipAt();
        const values = this.points().map(p => p.y ?? 0);
        const clipped = values.some(v => Math.abs(v) > limit);
        const y = options.scales!['y']!;
        Object.assign(y, {
            suggestedMin: -range,
            suggestedMax: range,
            // Only fixed when something goes beyond the limit, so the clipped values sit exactly on the edge
            min: values.some(v => v < -limit) ? -limit : undefined,
            max: values.some(v => v > limit) ? limit : undefined,
            grace: undefined,
            // With an edge fixed, Chart.js would count the ticks from it (-25, -15, -5...) and lose the zero line:
            // ticks every 10 anchored at zero instead
            afterBuildTicks: clipped
                ? (scale: { min: number; max: number; ticks: { value: number }[] }) => {
                      const ticks: { value: number }[] = [];
                      for (let v = Math.ceil(scale.min / 10) * 10; v <= scale.max; v += 10) ticks.push({ value: v });
                      scale.ticks = ticks;
                  }
                : undefined,
            // The zero line is the reference: stronger than the rest of the grid
            grid: {
                color: (ctx: ScriptableScaleContext) => (ctx.tick?.value === 0 ? CHART_CHROME.zero : CHART_CHROME.grid),
            },
        });
        // The legend dot of the bars would take the color of the first session: blue, as "better" is the reference
        options.plugins!.legend!.labels!.generateLabels = chart =>
            Chart.defaults.plugins.legend.labels.generateLabels(chart).map(item =>
                item.datasetIndex === 0 ? { ...item, fillStyle: DIVERGING.positive, strokeStyle: DIVERGING.positive } : item,
            );
        options.plugins!.legend!.labels!.filter = item => item.datasetIndex !== OUT_OF_SCALE;
        options.plugins!.tooltip!.filter = item => item.datasetIndex !== OUT_OF_SCALE;
        return options;
    });
}
