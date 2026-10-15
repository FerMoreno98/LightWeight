import { Component, computed, input } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions, TooltipItem } from 'chart.js';
import { CHART_CHROME } from '../chart-theme';
import { timeAxis } from './time-axis';

/** A value at a moment. The note (optional) is added to the tooltip */
export interface ChartPoint {
    /** Timestamp in ms */
    x: number;
    /** null when it was not measured: the line skips it */
    y: number | null;
    note?: string;
}

export interface ChartSeries {
    label: string;
    color: string;
    points: ChartPoint[];
    /** Width of the line in px (2 by default): a thinner one recedes behind a highlighted series */
    lineWidth?: number;
}

const dayMonth = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'short' });
const monthYear = new Intl.DateTimeFormat('es-ES', { month: 'short', year: '2-digit' });
const fullDate = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'long', year: 'numeric' });
const number = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 2 });

/**
 * Line chart over time, themed for the app. The X axis is linear over timestamps, so the distance
 * between points is the real time between measurements (no date adapter needed)
 */
@Component({
    selector: 'app-line-chart',
    standalone: true,
    imports: [BaseChartDirective],
    template: `
        <div class="relative h-64 sm:h-72">
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
            pointBackgroundColor: s.color,
            pointBorderColor: CHART_CHROME.surface,
            pointBorderWidth: 2,
        })),
    }));

    options = computed<ChartOptions<'line'>>(() => {
        const unit = this.unit();
        // Range and ticks calculated here: Chart.js' linear scale does not know the numbers are dates
        const axis = timeAxis(this.series().flatMap(s => s.points.map(p => p.x)));

        return {
            responsive: true,
            maintainAspectRatio: false,
            // Hover anywhere: the nearest measurement by date, with every series of that date
            interaction: { mode: 'nearest', axis: 'x', intersect: false },
            scales: {
                x: {
                    type: 'linear',
                    min: axis?.min,
                    max: axis?.max,
                    afterBuildTicks: scale => {
                        if (axis) scale.ticks = axis.ticks.map(value => ({ value }));
                    },
                    grid: { display: false },
                    border: { color: CHART_CHROME.grid },
                    ticks: {
                        color: CHART_CHROME.muted,
                        maxRotation: 0,
                        callback: value => (axis?.unit === 'month' ? monthYear : dayMonth).format(new Date(Number(value))),
                    },
                },
                y: {
                    grace: '10%',
                    grid: { color: CHART_CHROME.grid },
                    border: { display: false },
                    ticks: {
                        color: CHART_CHROME.muted,
                        maxTicksLimit: 6,
                        callback: value => `${number.format(Number(value))} ${unit}`,
                    },
                },
            },
            plugins: {
                // A single series is named by the card title; two or more need the legend
                legend: {
                    display: this.series().length > 1,
                    position: 'bottom',
                    labels: { color: CHART_CHROME.text, usePointStyle: true, pointStyle: 'circle', boxWidth: 8, boxHeight: 8, padding: 16 },
                },
                tooltip: {
                    backgroundColor: CHART_CHROME.tooltipBackground,
                    borderColor: CHART_CHROME.tooltipBorder,
                    borderWidth: 1,
                    titleColor: CHART_CHROME.text,
                    bodyColor: CHART_CHROME.text,
                    padding: 10,
                    usePointStyle: true,
                    boxWidth: 8,
                    boxHeight: 8,
                    callbacks: {
                        title: items => (items.length ? fullDate.format(new Date(items[0].parsed.x ?? 0)) : ''),
                        label: (item: TooltipItem<'line'>) => {
                            const point = item.raw as ChartPoint;
                            const value = `${item.dataset.label}: ${number.format(item.parsed.y ?? 0)} ${unit}`;
                            return point.note ? `${value} (${point.note})` : value;
                        },
                    },
                },
            },
        };
    });
}
