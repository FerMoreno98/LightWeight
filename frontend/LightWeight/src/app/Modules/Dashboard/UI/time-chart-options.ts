import { ChartOptions, TooltipItem } from 'chart.js';
import { CHART_CHROME } from './chart-theme';
import { timeAxis } from './line-chart/time-axis';

/** A value at a moment. Everything but x/y is optional extra information for the tooltip and the marks */
export interface ChartPoint {
    /** Timestamp in ms */
    x: number;
    /** null when it was not measured: the line skips it */
    y: number | null;
    /** Added after the value in the tooltip, between brackets */
    note?: string;
    /** Replaces the date as the title of the tooltip (e.g. "Torso A · 14 oct") */
    title?: string;
    /** Extra lines at the end of the tooltip (e.g. the wellbeing of the session) */
    details?: string[];
    /** Drawn as an empty ring: a value to read with care (e.g. a less precise estimation) */
    hollow?: boolean;
    /** Real value when y has been clipped to the edge of the chart; the tooltip shows this one */
    value?: number;
}

const dayMonth = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'short' });
const monthYear = new Intl.DateTimeFormat('es-ES', { month: 'short', year: '2-digit' });
const fullDate = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'long', year: 'numeric' });
const number = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 2 });
const signedNumber = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 1, signDisplay: 'exceptZero' });

/** Formats a value with its unit; signed values show "+" (e.g. "+2,4 %") */
export function formatValue(value: number, unit: string, signed = false): string {
    const formatted = (signed ? signedNumber : number).format(value);
    return unit ? `${formatted} ${unit}` : formatted;
}

export interface TimeChartSettings {
    /** Timestamps the X axis must cover: the chart's own points, or a shared list so stacked charts align */
    xs: number[];
    unit: string;
    /** Values shown with their sign ("+2,4 %") */
    signed?: boolean;
    /** Fixed Y range (e.g. 1-10); otherwise it follows the data */
    yRange?: { min: number; max: number };
    showLegend: boolean;
}

/**
 * Options shared by the line and bar charts over time: the time axis, the dark theme and a tooltip that can
 * show a custom title, a note and extra lines per point
 */
export function timeChartOptions(settings: TimeChartSettings): ChartOptions<'line' | 'bar'> {
    const { unit, signed = false, yRange, showLegend } = settings;
    // Range and ticks calculated here: Chart.js' linear scale does not know the numbers are dates
    const axis = timeAxis(settings.xs);

    return {
        responsive: true,
        maintainAspectRatio: false,
        // Hover anywhere: the nearest point by date, with every series of that date
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
                min: yRange?.min,
                max: yRange?.max,
                grace: yRange ? undefined : '10%',
                grid: { color: CHART_CHROME.grid },
                border: { display: false },
                ticks: {
                    color: CHART_CHROME.muted,
                    maxTicksLimit: 6,
                    callback: value => formatValue(Number(value), unit, signed),
                },
            },
        },
        plugins: {
            // A single series is named by the card title; two or more need the legend
            legend: {
                display: showLegend,
                position: 'bottom',
                labels: { color: CHART_CHROME.text, usePointStyle: true, pointStyle: 'circle', boxWidth: 8, boxHeight: 8, padding: 16 },
            },
            tooltip: {
                backgroundColor: CHART_CHROME.tooltipBackground,
                borderColor: CHART_CHROME.tooltipBorder,
                borderWidth: 1,
                titleColor: CHART_CHROME.text,
                bodyColor: CHART_CHROME.text,
                footerColor: CHART_CHROME.muted,
                footerFont: { weight: 'normal' },
                padding: 10,
                usePointStyle: true,
                boxWidth: 8,
                boxHeight: 8,
                callbacks: {
                    title: items => {
                        if (!items.length) return '';
                        const point = items[0].raw as ChartPoint;
                        return point.title ?? fullDate.format(new Date(point.x));
                    },
                    label: (item: TooltipItem<'line' | 'bar'>) => {
                        const point = item.raw as ChartPoint;
                        const value = `${item.dataset.label}: ${formatValue(point.value ?? point.y ?? 0, unit, signed)}`;
                        return point.note ? `${value} (${point.note})` : value;
                    },
                    footer: items => (items.length ? ((items[0].raw as ChartPoint).details ?? []) : []),
                },
            },
        },
    };
}
