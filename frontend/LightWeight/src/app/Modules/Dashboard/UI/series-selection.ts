import { computed, Signal, signal } from '@angular/core';
import { MAX_SERIES, SERIES_COLORS } from './chart-theme';

/** A field shown in a chart and the color slot it keeps while it stays selected */
export interface SelectedField {
    key: string;
    slot: number;
}

/**
 * The series chosen with chips in a chart. A new field takes the lowest free color and keeps it while selected,
 * so unselecting another one never repaints the rest; no more than MAX_SERIES at once (the colors validated
 * to be told apart). With nothing chosen, the default field (or the first one with data) is shown
 */
export class SeriesSelection {
    private chosen = signal<SelectedField[]>([]);

    /** The fields shown: the user's choice limited to the ones with data, or the default one */
    readonly selected: Signal<SelectedField[]>;

    /**
     * @param available Keys of the fields that have data (the others would draw an empty line)
     * @param defaultKey Field shown when nothing is chosen; the first available one if it has no data
     */
    constructor(available: Signal<string[]>, defaultKey: () => string | undefined) {
        this.selected = computed(() => {
            const keys = available();
            const valid = this.chosen().filter(s => keys.includes(s.key));
            if (valid.length > 0 || keys.length === 0) return valid;
            const fallback = defaultKey();
            return [{ key: fallback && keys.includes(fallback) ? fallback : keys[0], slot: 0 }];
        });
    }

    toggle(key: string): void {
        const fields = this.selected();
        if (fields.some(s => s.key === key)) {
            // The last one stays: a chart is never left empty
            if (fields.length > 1) this.chosen.set(fields.filter(s => s.key !== key));
            return;
        }
        if (fields.length >= MAX_SERIES) return;
        const slot = [...Array(MAX_SERIES).keys()].find(i => !fields.some(s => s.slot === i))!;
        this.chosen.set([...fields, { key, slot }]);
    }

    /** Color of the field if it is selected, null otherwise */
    colorOf(key: string): string | null {
        const selected = this.selected().find(s => s.key === key);
        return selected ? SERIES_COLORS[selected.slot] : null;
    }

    isFull(): boolean {
        return this.selected().length >= MAX_SERIES;
    }
}
