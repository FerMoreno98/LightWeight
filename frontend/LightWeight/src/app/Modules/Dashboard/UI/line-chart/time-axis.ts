/**
 * Range and ticks of a time axis over timestamps (ms).
 *
 * Chart.js' linear scale does not know these numbers are dates: with a single date it pads the range by 5%
 * of the value (years, for a timestamp) and puts the ticks on round numbers that are not round dates.
 * Here the range is padded by a few days and the ticks fall on days, Mondays or the 1st of a month,
 * in the local time of the user
 */
export interface TimeAxis {
    min: number;
    max: number;
    ticks: number[];
    /** How the ticks are labeled */
    unit: 'day' | 'month';
}

const DAY = 24 * 60 * 60 * 1000;
/** Padding at each side, so the first and last points are not stuck to the edges */
const MIN_PADDING = 2 * DAY;
/** Labels that fit in the width of a phone */
const MAX_TICKS = 6;

export function timeAxis(xs: number[]): TimeAxis | null {
    if (xs.length === 0) return null;
    const first = Math.min(...xs);
    const last = Math.max(...xs);
    const padding = Math.max((last - first) * 0.05, MIN_PADDING);
    const min = first - padding;
    const max = last + padding;
    const days = (max - min) / DAY;

    // Up to ~4 months: ticks on days (every N days); longer: on the 1st of every N months
    if (days <= 120) {
        const rawStep = Math.max(1, Math.ceil(days / MAX_TICKS));
        // Steps of a week or more are whole weeks starting on Monday, so every tick is a Monday
        const step = rawStep >= 7 ? Math.ceil(rawStep / 7) * 7 : rawStep;
        const firstDay = firstDayFrom(min);
        const start = step >= 7 ? nextMonday(firstDay) : firstDay;
        return { min, max, ticks: everyDays(start, max, step), unit: 'day' };
    }

    const months = days / 30;
    const step = Math.max(1, Math.ceil(months / MAX_TICKS));
    return { min, max, ticks: everyMonths(min, max, step), unit: 'month' };
}

/** First midnight (local) at or after the moment, so no tick falls before the start of the axis */
function firstDayFrom(ms: number): number {
    const d = new Date(ms);
    const midnight = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    return midnight.getTime() === ms
        ? ms
        : new Date(d.getFullYear(), d.getMonth(), d.getDate() + 1).getTime();
}

function nextMonday(dayStart: number): number {
    const d = new Date(dayStart);
    const daysToMonday = (8 - d.getDay()) % 7;
    return new Date(d.getFullYear(), d.getMonth(), d.getDate() + daysToMonday).getTime();
}

function everyDays(start: number, max: number, step: number): number[] {
    const ticks: number[] = [];
    const d = new Date(start);
    // Built with the calendar (not adding ms) so a change of daylight saving time does not move the ticks
    for (let i = 0; ; i += step) {
        const tick = new Date(d.getFullYear(), d.getMonth(), d.getDate() + i).getTime();
        if (tick > max) return ticks;
        ticks.push(tick);
    }
}

function everyMonths(min: number, max: number, step: number): number[] {
    const ticks: number[] = [];
    const d = new Date(min);
    for (let i = 1; ; i += step) {
        const tick = new Date(d.getFullYear(), d.getMonth() + i, 1).getTime();
        if (tick > max) return ticks;
        ticks.push(tick);
    }
}
