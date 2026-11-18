import { Plugin } from 'chart.js';
import { timeAxis } from './line-chart/time-axis';
import { Y_AXIS_WIDTH } from './time-chart-options';

export interface BarWidthSettings {
    /** Dates of the bars */
    xs: () => number[];
    /** Dates the X axis covers, when it is shared with another chart; the bars' own dates otherwise */
    domain?: () => number[] | undefined;
    /** Room at the right of the plot (layout.padding.right of the chart) */
    rightPadding: number;
    min: number;
    max: number;
    /** Share of the usual gap taken by a bar */
    share: number;
}

/**
 * Width of the bars of a time axis from the usual (median) gap between their dates. Chart.js takes the smallest gap
 * instead, so two dates very close (two sessions on one day, two weeks started together) turn every bar into a line.
 * Set before the update: Chart.js reads the width of the bars when the update starts, before laying out the scales
 */
export function barWidthPlugin(settings: BarWidthSettings): Plugin<'bar'> {
    return {
        id: 'barWidth',
        beforeUpdate: chart => {
            const xs = [...new Set(settings.xs())].sort((a, b) => a - b);
            const gaps = xs.slice(1).map((v, i) => v - xs[i]).sort((a, b) => a - b);
            const axis = timeAxis(settings.domain?.() ?? xs);
            if (!axis || gaps.length === 0) return;
            const gap = gaps[Math.floor(gaps.length / 2)];
            const plotWidth = chart.width - Y_AXIS_WIDTH - settings.rightPadding;
            const pixels = (gap / (axis.max - axis.min)) * plotWidth * settings.share;
            const width = Math.round(Math.max(settings.min, Math.min(settings.max, pixels)));
            for (const dataset of chart.data.datasets) {
                if ((dataset.type ?? 'bar') === 'bar') (dataset as { barThickness?: number }).barThickness = width;
            }
        },
    };
}
