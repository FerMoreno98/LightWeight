import { Component, computed, input } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions, Plugin } from 'chart.js';
import { barWidthPlugin } from '../bar-width';
import { CHART_CHROME } from '../chart-theme';
import { ChartPoint, formatValue, timeChartOptions } from '../time-chart-options';
import { ChartSeries } from '../line-chart/line-chart';

/**
 * Stacked bars over time: the height is the sum of the series on each date and each color its share (e.g. the sets
 * of each muscle group in a week). A 2px gap in the color of the card separates the segments
 */
@Component({
    selector: 'app-stacked-bar-chart',
    standalone: true,
    imports: [BaseChartDirective],
    template: `
        <div class="relative h-64 sm:h-72">
            <canvas baseChart type="bar" [data]="data()" [options]="options()" [plugins]="plugins" [attr.aria-label]="ariaLabel()" role="img"></canvas>
        </div>
    `,
})
export class StackedBarChart {
    /** Every series must have a point on the same dates */
    series = input.required<ChartSeries[]>();
    unit = input.required<string>();
    ariaLabel = input.required<string>();

    data = computed<ChartData<'bar', ChartPoint[]>>(() => {
        const series = this.series();
        return {
            datasets: series.map((s, i) => ({
                label: s.label,
                data: s.points,
                backgroundColor: s.color,
                borderColor: CHART_CHROME.surface,
                borderWidth: { top: 2, right: 0, bottom: 0, left: 0 },
                // Rounded only at the top of the stack
                borderRadius: i === series.length - 1 ? { topLeft: 4, topRight: 4, bottomLeft: 0, bottomRight: 0 } : 0,
                borderSkipped: false,
                // Set by barWidthPlugin from the usual gap between weeks
                barThickness: 28,
            })),
        };
    });

    readonly plugins: Plugin<'bar'>[] = [
        barWidthPlugin({ xs: () => this.series().flatMap(s => s.points.map(p => p.x)), rightPadding: 8, min: 4, max: 28, share: 0.7 }),
    ];

    options = computed(() => {
        const series = this.series();
        const unit = this.unit();
        const options = timeChartOptions({
            xs: series.flatMap(s => s.points.map(p => p.x)),
            unit,
            showLegend: series.length > 1,
        }) as ChartOptions<'bar'>;
        options.scales!['x']!.stacked = true;
        Object.assign(options.scales!['y']!, { stacked: true, beginAtZero: true });
        // Every segment of the bar under the finger, not only the nearest one
        options.interaction = { mode: 'index', intersect: false };
        options.plugins!.crosshair = { enabled: false };
        const tooltip = options.plugins!.tooltip!;
        tooltip.footerColor = CHART_CHROME.text;
        tooltip.callbacks!.footer = items =>
            items.length > 1 ? `Total: ${formatValue(items.reduce((sum, item) => sum + (item.parsed.y ?? 0), 0), unit)}` : '';
        return options;
    });
}
