import { Component, computed, input } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions, Plugin, ScriptableScaleContext } from 'chart.js';
import { CHART_CHROME, DISPLAY_FONT } from '../chart-theme';
import { formatValue } from '../time-chart-options';

/** How much a measure has changed between its first and its last value */
export interface ChangeItem {
    label: string;
    /** Same color as its line in the evolution chart */
    color: string;
    from: number;
    to: number;
    /** Dates of the first and the last value, for the tooltip */
    fromDate: string;
    toDate: string;
}

const ROW_HEIGHT = 40;
const AXIS_HEIGHT = 32;

/**
 * Horizontal bars of the change of each measure, from zero, with the value written at the end of each bar.
 * A summary of the period that complements the evolution chart, not a replacement
 */
@Component({
    selector: 'app-change-bars',
    standalone: true,
    imports: [BaseChartDirective],
    template: `
        <div class="relative" [style.height.px]="height()">
            <canvas baseChart type="bar" [data]="data()" [options]="options()" [plugins]="plugins" [attr.aria-label]="ariaLabel()" role="img"></canvas>
        </div>
    `,
})
export class ChangeBars {
    items = input.required<ChangeItem[]>();
    unit = input.required<string>();
    ariaLabel = input.required<string>();

    height = computed(() => this.items().length * ROW_HEIGHT + AXIS_HEIGHT);

    private changes = computed(() => this.items().map(i => Math.round((i.to - i.from) * 10) / 10));

    data = computed<ChartData<'bar', number[]>>(() => ({
        labels: this.items().map(i => i.label),
        datasets: [
            {
                label: 'Cambio',
                data: this.changes(),
                backgroundColor: this.items().map(i => i.color),
                borderRadius: 4,
                borderSkipped: false,
                barThickness: 20,
            },
        ],
    }));

    options = computed<ChartOptions<'bar'>>(() => {
        const unit = this.unit();
        const items = this.items();
        // Symmetric around zero, with room for the labels at the end of the bars
        const reach = Math.max(1, ...this.changes().map(Math.abs)) * 1.45;
        return {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'nearest', axis: 'y', intersect: false },
            scales: {
                x: {
                    min: -reach,
                    max: reach,
                    grid: { color: (ctx: ScriptableScaleContext) => (ctx.tick?.value === 0 ? CHART_CHROME.zero : CHART_CHROME.grid) },
                    border: { display: false },
                    ticks: {
                        color: CHART_CHROME.muted,
                        font: { family: DISPLAY_FONT, size: 13, weight: 500 },
                        maxTicksLimit: 5,
                        callback: value => formatValue(Number(value), unit, true),
                    },
                },
                y: {
                    grid: { display: false },
                    border: { display: false },
                    ticks: { color: CHART_CHROME.text, font: { size: 13, weight: 500 } },
                },
            },
            plugins: {
                legend: { display: false },
                crosshair: { enabled: false },
                tooltip: {
                    backgroundColor: CHART_CHROME.tooltipBackground,
                    borderColor: CHART_CHROME.tooltipBorder,
                    borderWidth: 1,
                    titleColor: CHART_CHROME.text,
                    titleFont: { family: DISPLAY_FONT, size: 15, weight: 600 },
                    bodyColor: CHART_CHROME.text,
                    footerColor: CHART_CHROME.muted,
                    footerFont: { weight: 'normal' },
                    padding: { x: 12, y: 10 },
                    cornerRadius: 8,
                    caretSize: 0,
                    displayColors: false,
                    callbacks: {
                        label: item => {
                            const i = items[item.dataIndex];
                            return `${formatValue(i.from, unit)} → ${formatValue(i.to, unit)} (${formatValue(item.parsed.x ?? 0, unit, true)})`;
                        },
                        footer: tooltipItems => {
                            const i = items[tooltipItems[0]?.dataIndex];
                            return i ? `${i.fromDate} → ${i.toDate}` : '';
                        },
                    },
                },
            },
        };
    });

    /** The change written at the end of each bar: outside it, at the left of the negative ones */
    readonly plugins: Plugin<'bar'>[] = [
        {
            id: 'changeValues',
            afterDatasetsDraw: chart => {
                const { ctx } = chart;
                const changes = this.changes();
                ctx.save();
                ctx.font = `600 14px ${DISPLAY_FONT}`;
                ctx.fillStyle = CHART_CHROME.text;
                ctx.textBaseline = 'middle';
                chart.getDatasetMeta(0).data.forEach((bar, i) => {
                    const value = changes[i] ?? 0;
                    ctx.textAlign = value < 0 ? 'right' : 'left';
                    ctx.fillText(formatValue(value, this.unit(), true), bar.x + (value < 0 ? -8 : 8), bar.y);
                });
                ctx.restore();
            },
        },
    ];
}
