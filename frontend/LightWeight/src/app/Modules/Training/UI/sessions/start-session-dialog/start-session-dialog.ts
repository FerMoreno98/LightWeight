import { Component, effect, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

export interface StartSessionForm {
  motivationLevel: number;
  sleepLevel: number;
  domsLevel: number;
  comments: string | null;
}

interface LevelField {
  key: 'motivationLevel' | 'sleepLevel' | 'domsLevel';
  label: string;
  hint: string;
}

/** Modal with the questions asked before starting a session (motivation, sleep, soreness and notes) */
@Component({
  selector: 'app-start-session-dialog',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './start-session-dialog.html',
  styleUrl: './start-session-dialog.css',
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
})
export class StartSessionDialog {
  open = input(false);
  sessionName = input('');
  busy = input(false);

  confirmed = output<StartSessionForm>();
  cancelled = output<void>();

  readonly levels = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
  readonly fields: LevelField[] = [
    { key: 'motivationLevel', label: 'Motivación', hint: '1 = ninguna · 10 = máxima' },
    { key: 'sleepLevel', label: 'Calidad del sueño', hint: '1 = muy mala · 10 = perfecta' },
    { key: 'domsLevel', label: 'Agujetas', hint: '1 = ninguna · 10 = muchísimas' },
  ];

  values = signal({ motivationLevel: 7, sleepLevel: 7, domsLevel: 3 });
  comments = '';

  constructor() {
    // Every time it opens it starts from the default answers
    effect(() => {
      if (this.open()) {
        this.values.set({ motivationLevel: 7, sleepLevel: 7, domsLevel: 3 });
        this.comments = '';
      }
    });
  }

  select(field: LevelField['key'], level: number) {
    this.values.update(v => ({ ...v, [field]: level }));
  }

  confirm() {
    if (this.busy()) return;
    const comments = this.comments.trim();
    this.confirmed.emit({ ...this.values(), comments: comments || null });
  }

  cancel() {
    if (!this.busy()) this.cancelled.emit();
  }

  onEscape() {
    if (this.open()) this.cancel();
  }
}
