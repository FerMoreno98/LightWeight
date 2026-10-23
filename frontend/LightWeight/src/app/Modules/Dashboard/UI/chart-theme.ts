/**
 * Colors of the charts. Canvas cannot read CSS classes, so they live here.
 *
 * Series: categorical palette in a fixed order, validated against the card surface (slate-800) for
 * color blindness. Only the first four are used at once: they are the ones that pass every check
 * together, so a selector never shows more than four series.
 */
export const SERIES_COLORS = ['#3987e5', '#d95926', '#199e70', '#c98500'] as const;

/** Maximum series in a chart at the same time */
export const MAX_SERIES = SERIES_COLORS.length;

/**
 * Values around zero (better / worse than before): the blue and red poles of the validated dark palette.
 * Always shown with their sign too, so the meaning never depends on the color alone
 */
export const DIVERGING = {
    positive: '#3987e5',
    negative: '#e66767',
} as const;

export const CHART_CHROME = {
    /** Card background (slate-800): ring around the points so overlapping ones stay apart */
    surface: '#1e293b',
    /** Axis labels and ticks (slate-400) */
    muted: '#94a3b8',
    /** Legend and tooltip text (slate-200) */
    text: '#e2e8f0',
    grid: 'rgba(148, 163, 184, 0.12)',
    tooltipBackground: '#0f172a',
    tooltipBorder: '#334155',
} as const;
