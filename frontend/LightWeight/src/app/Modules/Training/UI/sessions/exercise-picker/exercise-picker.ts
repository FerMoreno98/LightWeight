import { Component, computed, effect, input, output, signal } from '@angular/core';
import { Exercise } from '../../../data/training-api.service';

/** Modal to choose an exercise of the catalog, with a search box */
@Component({
  selector: 'app-exercise-picker',
  standalone: true,
  templateUrl: './exercise-picker.html',
  styleUrl: './exercise-picker.css',
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
})
export class ExercisePicker {
  open = input(false);
  title = input('Elige un ejercicio');
  exercises = input<Exercise[]>([]);
  /** Exercise that cannot be chosen (e.g. the one being replaced) */
  excludedId = input<string | null>(null);

  picked = output<Exercise>();
  cancelled = output<void>();

  search = signal('');

  filtered = computed(() => {
    const term = normalize(this.search());
    return this.exercises()
      .filter(e => e.id !== this.excludedId())
      .filter(e => !term || normalize(e.name).includes(term))
      .sort((a, b) => a.name.localeCompare(b.name, 'es'));
  });

  constructor() {
    effect(() => {
      if (this.open()) this.search.set('');
    });
  }

  onSearch(event: Event) {
    this.search.set((event.target as HTMLInputElement).value);
  }

  onEscape() {
    if (this.open()) this.cancelled.emit();
  }
}

/** Lower case without accents, so "press" finds "Press" and "biceps" finds "Bíceps" */
function normalize(text: string): string {
  return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
}
