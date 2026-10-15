import { timeAxis } from './time-axis';

const DAY = 24 * 60 * 60 * 1000;
const at = (y: number, m: number, d: number, h = 8) => new Date(y, m - 1, d, h).getTime();

describe('timeAxis', () => {
    it('returns null without points', () => {
        expect(timeAxis([])).toBeNull();
    });

    it('with a single point, the range is a few days around it (not years)', () => {
        const today = at(2026, 10, 15);
        const axis = timeAxis([today])!;

        expect(axis.min).toBe(today - 2 * DAY);
        expect(axis.max).toBe(today + 2 * DAY);
        expect(axis.unit).toBe('day');
        // Every tick is a local midnight inside the range, around the 15th of October
        for (const tick of axis.ticks) {
            const d = new Date(tick);
            expect(d.getHours()).toBe(0);
            expect(d.getMonth()).toBe(9);
            expect(tick).toBeGreaterThanOrEqual(axis.min);
            expect(tick).toBeLessThanOrEqual(axis.max);
        }
        expect(axis.ticks.map(t => new Date(t).getDate())).toEqual([14, 15, 16, 17]);
    });

    it('over some weeks, the ticks are Mondays', () => {
        const axis = timeAxis([at(2026, 8, 20), at(2026, 10, 15)])!;

        expect(axis.unit).toBe('day');
        expect(axis.ticks.length).toBeGreaterThan(1);
        expect(axis.ticks.length).toBeLessThanOrEqual(7);
        for (const tick of axis.ticks) {
            expect(new Date(tick).getDay()).toBe(1);
        }
    });

    it('over months, the ticks are the 1st of a month', () => {
        const axis = timeAxis([at(2025, 3, 10), at(2026, 10, 15)])!;

        expect(axis.unit).toBe('month');
        expect(axis.ticks.length).toBeLessThanOrEqual(7);
        for (const tick of axis.ticks) {
            const d = new Date(tick);
            expect(d.getDate()).toBe(1);
            expect(tick).toBeGreaterThanOrEqual(axis.min);
            expect(tick).toBeLessThanOrEqual(axis.max);
        }
    });
});
