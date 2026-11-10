/**
 * Colors of the charts. Canvas cannot read CSS classes, so the chrome reads the design tokens of
 * styles.css as CSS variables (the values after `??` are only a fallback in case they are not loaded).
 *
 * Series: categorical palette in a fixed order, validated for color blindness against a dark card
 * surface. Only the first four are used at once: they are the ones that pass every check together,
 * so a selector never shows more than four series.
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

function token(name: string, fallback: string): string {
    if (typeof document === 'undefined') return fallback;
    return getComputedStyle(document.documentElement).getPropertyValue(`--color-${name}`).trim() || fallback;
}

export const CHART_CHROME = {
    /** Card background: ring around the points so overlapping ones stay apart */
    surface: token('surface', '#161d1a'),
    /** Axis labels and ticks */
    muted: token('muted', '#a3b1ab'),
    /** Legend and tooltip text */
    text: token('fg', '#e9efec'),
    grid: 'rgba(163, 177, 171, 0.12)',
    tooltipBackground: token('canvas', '#0f1412'),
    tooltipBorder: token('line-strong', '#3f4e48'),
} as const;

/** Same text font as the rest of the app (the canvas does not inherit it from CSS) */
export const CHART_DEFAULTS = {
    font: { family: "'Figtree', 'Segoe UI', system-ui, sans-serif" },
};
