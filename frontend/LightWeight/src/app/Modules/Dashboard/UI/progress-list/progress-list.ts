import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ExerciseProgressStore } from '../../state/exercise-progress.store';
import { formatValue } from '../time-chart-options';

const DAY_MS = 24 * 60 * 60 * 1000;
const kg = new Intl.NumberFormat('es-ES', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

/** Progreso: every exercise the user has performed, with its current estimated 1RM and its trend */
@Component({
    selector: 'app-progress-list',
    standalone: true,
    imports: [RouterLink],
    templateUrl: './progress-list.html',
})
export class ProgressList implements OnInit {
    store = inject(ExerciseProgressStore);
    search = signal('');

    /** Matches ignoring case and accents ("press banca" finds "Press Banca", "triceps" finds "Tríceps") */
    exercises = computed(() => {
        const term = normalize(this.search().trim());
        return term ? this.store.exercises().filter(e => normalize(e.name).includes(term)) : this.store.exercises();
    });

    ngOnInit() {
        this.store.LoadExercises();
    }

    daysAgo(date: string): string {
        const days = Math.floor((Date.now() - Date.parse(date)) / DAY_MS);
        if (days <= 0) return 'hoy';
        if (days === 1) return 'ayer';
        return `hace ${days} días`;
    }

    oneRepMax(value: number): string {
        return `${kg.format(value)} kg`;
    }

    /** "↑ +2,5 %", "↓ −1,8 %" or "= 0 %": arrow and number, so it never depends on the color alone */
    trend(value: number): string {
        const arrow = value > 0 ? '↑' : value < 0 ? '↓' : '=';
        return `${arrow} ${formatValue(value, '%', true)}`;
    }

    trendClass(value: number): string {
        return value > 0 ? 'text-sky-400' : value < 0 ? 'text-rose-400' : 'text-slate-400';
    }
}

function normalize(text: string): string {
    return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}
