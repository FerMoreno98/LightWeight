import { Component, computed, input } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions, ScriptableContext } from 'chart.js';
import { CHART_CHROME } from '../chart-theme';
import { ChartPoint, timeChartOptions } from '../time-chart-options';

export type { ChartPoint } from '../time-chart-options';

export interface ChartSeries {
    label: string;
    color: string;
    points: ChartPoint[];
    /** Width of the line in px (2 by default): a thinner one recedes behind a highlighted series */
    lineWidth?: number;
}

/**
 * Line chart over time, themed for the app. The X axis is linear over timestamps, so the distance
 * between points is the real time between measurements (no date adapter needed)
 */
@Component({
    selector: 'app-line-chart',
    standalone: true,
    imports: [BaseChartDirective],
    template: `
        <div class="relative" [class]="compact() ? 'h-44 sm:h-48' : 'h-64 sm:h-72'">
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
    /** Lower chart, for a chart stacked under another */
    compact = input(false);

    data = computed<ChartData<'line', ChartPoint[]>>(() => ({
        datasets: this.series().map(s => ({
            label: s.label,
            data: s.points,
            borderColor: s.color,
            backgroundColor: s.color,
            borderWidth: s.lineWidth ?? 2,
            spanGaps: true,
            tension: 0.25,
            pointRadius: 4,
            pointHoverRadius: 6,
            // A hollow point is an empty ring in the color of its series
            pointBackgroundColor: (ctx: ScriptableContext<'line'>) => ((ctx.raw as ChartPoint | undefined)?.hollow ? CHART_CHROME.surface : s.color),
            pointBorderColor: (ctx: ScriptableContext<'line'>) => ((ctx.raw as ChartPoint | undefined)?.hollow ? s.color : CHART_CHROME.surface),
            pointBorderWidth: 2,
        })),
    }));

    options = computed(() => timeChartOptions({
        xs: this.xDomain() ?? this.series().flatMap(s => s.points.map(p => p.x)),
        unit: this.unit(),
        signed: this.signed(),
        yRange: this.yRange(),
        showLegend: this.series().length > 1,
    }) as ChartOptions<'line'>);
}
