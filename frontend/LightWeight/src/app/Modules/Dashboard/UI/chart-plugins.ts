import { Chart, ChartType, Filler, Plugin } from 'chart.js';
import { CHART_CHROME, DISPLAY_FONT } from './chart-theme';

export interface EndLabelOptions {
    /** Indexes of the datasets whose last value is written next to the end of the line */
    datasets?: number[];
    format?: (value: number) => string;
}

declare module 'chart.js' {
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    interface PluginOptionsByType<TType extends ChartType> {
        crosshair?: { enabled?: boolean };
        endLabel?: EndLabelOptions;
        referenceBand?: ReferenceBandOptions;
    }
}

/** Dashed vertical line at the date under the finger or the pointer, so every series of that date is read on it */
export const CrosshairPlugin: Plugin = {
    id: 'crosshair',
    defaults: { enabled: true },
    afterDatasetsDraw(chart, _args, options: { enabled?: boolean }) {
        if (!options.enabled) return;
        const active = chart.tooltip?.getActiveElements() ?? [];
        if (active.length === 0) return;
        const { ctx, chartArea } = chart;
        const x = active[0].element.x;
        ctx.save();
        ctx.strokeStyle = CHART_CHROME.tooltipBorder;
        ctx.lineWidth = 1;
        ctx.setLineDash([3, 3]);
        ctx.beginPath();
        ctx.moveTo(x, chartArea.top);
        ctx.lineTo(x, chartArea.bottom);
        ctx.stroke();
        ctx.restore();
    },
};

/** Minimum vertical distance between two end labels, so they never overlap */
const LABEL_GAP = 15;

/**
 * The last value of some lines written next to their end, so the current value is read without hovering.
 * The chart needs room at the right (layout.padding.right); labels too close are pushed apart
 */
export const EndLabelPlugin: Plugin = {
    id: 'endLabel',
    afterDatasetsDraw(chart: Chart, _args, options: EndLabelOptions) {
        const format = options.format ?? (v => String(v));
        const labels: { x: number; y: number; text: string }[] = [];
        for (const index of options.datasets ?? []) {
            if (!chart.isDatasetVisible(index)) continue;
            const data = chart.data.datasets[index]?.data ?? [];
            const elements = chart.getDatasetMeta(index).data;
            for (let i = data.length - 1; i >= 0; i--) {
                const raw = data[i] as { y: number | null; value?: number } | number | null;
                const value = typeof raw === 'number' ? raw : raw?.value ?? raw?.y;
                if (value === null || value === undefined || !elements[i]) continue;
                labels.push({ x: elements[i].x, y: elements[i].y, text: format(value) });
                break;
            }
        }
        labels.sort((a, b) => a.y - b.y);
        for (let i = 1; i < labels.length; i++) labels[i].y = Math.max(labels[i].y, labels[i - 1].y + LABEL_GAP);

        const { ctx } = chart;
        ctx.save();
        ctx.font = `600 14px ${DISPLAY_FONT}`;
        ctx.fillStyle = CHART_CHROME.text;
        ctx.textBaseline = 'middle';
        for (const label of labels) ctx.fillText(label.text, label.x + 8, label.y);
        ctx.restore();
    },
};

export interface ReferenceBandOptions {
    /** Y values of the band; without them nothing is drawn */
    min?: number;
    max?: number;
    color?: string;
}

/** A horizontal band behind the lines: an orientative range (e.g. 1,6-2,2 g/kg of protein) */
export const ReferenceBandPlugin: Plugin = {
    id: 'referenceBand',
    beforeDatasetsDraw(chart: Chart, _args, options: ReferenceBandOptions) {
        if (options.min === undefined || options.max === undefined) return;
        const y = chart.scales['y'];
        if (!y) return;
        const { ctx, chartArea } = chart;
        const top = Math.max(chartArea.top, y.getPixelForValue(options.max));
        const bottom = Math.min(chartArea.bottom, y.getPixelForValue(options.min));
        if (bottom <= top) return;
        ctx.save();
        ctx.fillStyle = options.color ?? 'rgba(163, 177, 171, 0.1)';
        ctx.fillRect(chartArea.left, top, chartArea.right - chartArea.left, bottom - top);
        ctx.restore();
    },
};

/** Common to every chart: the areas under the lines, the vertical line on hover, the end labels and the bands */
export const CHART_PLUGINS = [Filler, CrosshairPlugin, EndLabelPlugin, ReferenceBandPlugin];
