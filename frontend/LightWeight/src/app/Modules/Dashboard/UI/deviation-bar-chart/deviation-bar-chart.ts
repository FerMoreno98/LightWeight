import { Component, computed, input } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions, Plugin, ScriptableContext, ScriptableScaleContext } from 'chart.js';
import { CHART_CHROME, withAlpha } from '../chart-theme';
import { ChartPoint, END_LABEL_ROOM, timeChartOptions } from '../time-chart-options';
import { barWidthPlugin } from '../bar-width';

const MIN_BAR = 4;
const MAX_BAR = 18;
const BAR_SHARE = 0.6;

/**
 * Values over time as bars above or below a reference (zero = the user's own average), in one color: up is more
 * than usual, down is less, with the sign in the labels. Each bar takes the opacity of its point's `strength`, so a
 * value to trust less (a week with few days of data) is lighter. Same time axis and widths as the line chart, so it
 * can be stacked under it
 */
@Component({
    selector: 'app-deviation-bar-chart',
    standalone: true,
    imports: [BaseChartDirective],
    template: `
        <div class="relative h-32 sm:h-36">
            <canvas baseChart type="bar" [data]="data()" [options]="options()" [plugins]="plugins" [attr.aria-label]="ariaLabel()" role="img"></canvas>
        </div>
    `,
})
export class DeviationBarChart {
    label = input.required<string>();
    points = input.required<ChartPoint[]>();
    unit = input.required<string>();
    color = input.required<string>();
    ariaLabel = input.required<string>();
    /** Timestamps the X axis must cover: the same list as the chart above, so each week falls on the same vertical */
    xDomain = input<number[] | undefined>(undefined);
    /** The axis always covers at least ±this value, so a small difference does not look like a big one */
    minRange = input(150);

    data = computed<ChartData<'bar', ChartPoint[]>>(() => {
        const strength = (ctx: ScriptableContext<'bar'>) => (ctx.raw as ChartPoint | undefined)?.strength ?? 1;
        return {
            datasets: [
                {
                    label: this.label(),
                    data: this.points(),
                    backgroundColor: ctx => withAlpha(this.color(), Math.max(0.2, strength(ctx))),
                    hoverBackgroundColor: this.color(),
                    // The lightest bars keep an outline, so they are still seen on the dark card
                    borderColor: ctx => (strength(ctx) < 0.4 ? withAlpha(this.color(), 0.7) : 'transparent'),
                    borderWidth: 1,
                    borderRadius: 3,
                    barThickness: MAX_BAR,
                },
            ],
        } as ChartData<'bar', ChartPoint[]>;
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
            showLegend: false,
        }) as ChartOptions<'bar'>;
        // Same room at the right as a chart with end labels, so the dates line up with the chart above
        options.layout = { padding: { top: 4, right: END_LABEL_ROOM } };
        const range = this.minRange();
        Object.assign(options.scales!['y']!, {
            suggestedMin: -range,
            suggestedMax: range,
            grace: undefined,
            ticks: { ...options.scales!['y']!.ticks, maxTicksLimit: 3 },
            // Zero is the user's average: the reference, stronger than the rest of the grid
            grid: { color: (ctx: ScriptableScaleContext) => (ctx.tick?.value === 0 ? CHART_CHROME.zero : CHART_CHROME.grid) },
        });
        return options;
    });
}
