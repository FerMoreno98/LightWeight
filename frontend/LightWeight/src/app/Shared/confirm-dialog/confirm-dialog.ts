import { Component, ElementRef, effect, input, output, viewChild } from '@angular/core';

/**
 * Confirmation modal with the app styles, used instead of the browser's confirm().
 * The parent controls whether it is open and reacts to (confirmed) / (cancelled).
 */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.css',
  host: {
    '(document:keydown.escape)': 'onEscape()',
  },
})
export class ConfirmDialog {
  open = input(false);
  title = input.required<string>();
  message = input('');
  confirmLabel = input('Confirmar');
  cancelLabel = input('Cancelar');
  /** Red confirm button for destructive or irreversible actions */
  danger = input(true);
  /** Disables the buttons while the parent runs the action */
  busy = input(false);

  confirmed = output<void>();
  cancelled = output<void>();

  private cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');

  constructor() {
    // Focus "Cancelar" when the modal opens, so Enter never confirms an irreversible action by accident
    effect(() => {
      if (this.open()) this.cancelButton()?.nativeElement.focus();
    });
  }

  confirm() {
    if (!this.busy()) this.confirmed.emit();
  }

  cancel() {
    if (!this.busy()) this.cancelled.emit();
  }

  onEscape() {
    if (this.open()) this.cancel();
  }
}
