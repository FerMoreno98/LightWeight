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
    /** Weekly weight average: the protagonist of its chart */
    accent: token('accent', '#3ddc97'),
    grid: 'rgba(163, 177, 171, 0.12)',
    /** Zero line of the charts around zero: stronger than the rest of the grid */
    zero: token('muted', '#a3b1ab'),
    tooltipBackground: token('raised', '#1e2723'),
    tooltipBorder: token('line-strong', '#3f4e48'),
} as const;

/** Numbers of the axes and the labels drawn on the canvas, like the numbers of the rest of the app */
export const DISPLAY_FONT = "'Barlow Condensed', 'Arial Narrow', system-ui, sans-serif";

/** A #rrggbb color with transparency */
export function withAlpha(hex: string, alpha: number): string {
    const n = parseInt(hex.slice(1, 7), 16);
    if (!/^#[0-9a-f]{6}$/i.test(hex) || Number.isNaN(n)) return hex;
    return `rgba(${n >> 16}, ${(n >> 8) & 255}, ${n & 255}, ${alpha})`;
}

/** Same text font as the rest of the app (the canvas does not inherit it from CSS) */
export const CHART_DEFAULTS = {
    font: { family: "'Figtree', 'Segoe UI', system-ui, sans-serif" },
};
