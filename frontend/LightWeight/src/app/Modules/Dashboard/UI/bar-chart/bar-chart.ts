import { Component, computed, input } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions, ScriptableContext, ScriptableScaleContext } from 'chart.js';
import { CHART_CHROME, DIVERGING } from '../chart-theme';
import { ChartPoint, timeChartOptions } from '../time-chart-options';

/**
 * Values over time around zero, drawn as "lollipops": a thin bar from zero and a dot at its value. Blue up
 * (better), red down (worse), and the sign is also in the labels, so the meaning never depends on the color alone.
 * The dot keeps a value of 0 visible (a bar of height 0 would disappear). Same time axis as the line chart, so both
 * can be stacked
 */
@Component({
    selector: 'app-bar-chart',
    standalone: true,
    imports: [BaseChartDirective],
    template: `
        <div class="relative h-56 sm:h-64">
            <canvas baseChart type="bar" [data]="data()" [options]="options()" [attr.aria-label]="ariaLabel()" role="img"></canvas>
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

    /** The points as drawn: the ones beyond the limit are moved to the edge and keep their real value */
    private drawn = computed<ChartPoint[]>(() => {
        const limit = this.clipAt();
        return this.points().map(p => {
            if (p.y === null || Math.abs(p.y) <= limit) return p;
            const edge = Math.sign(p.y) * limit;
            return { ...p, y: edge, value: p.y, note: p.note ? `${p.note}; fuera de escala` : 'fuera de escala' };
        });
    });

    // A mixed chart (bar + line datasets): Chart.js supports it through the type of each dataset, but the typing of
    // the directive expects a single type, hence the cast below
    data = computed<ChartData<'bar', ChartPoint[]>>(() => {
        const color = (ctx: ScriptableContext<'bar'> | ScriptableContext<'line'>) =>
            ((ctx.raw as ChartPoint | undefined)?.y ?? 0) < 0 ? DIVERGING.negative : DIVERGING.positive;
        return {
            datasets: [
                {
                    type: 'bar',
                    label: this.label(),
                    data: this.drawn(),
                    backgroundColor: color,
                    maxBarThickness: 4,
                    borderRadius: 2,
                },
                {
                    // The head of the lollipop: only the points, no line
                    type: 'line',
                    label: this.label(),
                    data: this.drawn(),
                    showLine: false,
                    // The triangle of a clipped value sits on the edge: drawn even if it sticks out of the chart area
                    clip: false,
                    // A clipped value ends in a triangle pointing out of the chart (down when negative)
                    pointStyle: (ctx: ScriptableContext<'line'>) => ((ctx.raw as ChartPoint | undefined)?.value !== undefined ? 'triangle' : 'circle'),
                    rotation: (ctx: ScriptableContext<'line'>) => (((ctx.raw as ChartPoint | undefined)?.y ?? 0) < 0 ? 180 : 0),
                    pointRadius: (ctx: ScriptableContext<'line'>) => ((ctx.raw as ChartPoint | undefined)?.value !== undefined ? 7 : 5),
                    pointHoverRadius: 7,
                    pointBackgroundColor: color,
                    pointBorderColor: CHART_CHROME.surface,
                    pointBorderWidth: 2,
                },
            ],
        } as unknown as ChartData<'bar', ChartPoint[]>;
    });

    options = computed(() => {
        const options = timeChartOptions({
            xs: this.xDomain() ?? this.points().map(p => p.x),
            unit: this.unit(),
            signed: true,
            showLegend: false,
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
                color: (ctx: ScriptableScaleContext) => (ctx.tick?.value === 0 ? CHART_CHROME.muted : CHART_CHROME.grid),
            },
        });
        // The bar and its dot are the same value: the tooltip shows it once
        options.plugins!.tooltip!.filter = item => item.datasetIndex === 0;
        return options;
    });
}
