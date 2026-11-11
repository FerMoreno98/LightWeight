import { Component, ElementRef, inject, input, signal } from '@angular/core';

let nextId = 0;

/**
 * Small "i" button that shows a short explanation. It opens on click/tap (hover does not exist on mobile)
 * and closes when clicking outside or pressing Escape
 */
@Component({
  selector: 'app-info-tooltip',
  standalone: true,
  host: {
    class: 'relative inline-flex',
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'close()',
  },
  template: `
    <button
      type="button"
      (click)="$event.stopPropagation(); toggle()"
      [attr.aria-expanded]="open()"
      [attr.aria-describedby]="open() ? id : null"
      [attr.aria-label]="label()"
      class="inline-flex h-5 w-5 items-center justify-center rounded-full text-subtle transition hover:text-accent focus-visible:text-accent"
    >
      <svg class="h-4 w-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24" aria-hidden="true">
        <circle cx="12" cy="12" r="9" />
        <path stroke-linecap="round" d="M12 11v5M12 8h.01" />
      </svg>
    </button>
    @if (open()) {
      <span
        [id]="id"
        role="tooltip"
        class="absolute left-1/2 top-full z-30 mt-2 w-64 max-w-[calc(100vw-2rem)] -translate-x-1/2 rounded-lg border border-line bg-raised px-3 py-2 text-left text-xs font-normal normal-case leading-relaxed tracking-normal text-soft shadow-lg shadow-black/40"
      >
        {{ text() }}
      </span>
    }
  `,
})
export class InfoTooltip {
  /** Explanation shown in the bubble */
  text = input.required<string>();
  /** Accessible name of the button */
  label = input('Más información');

  open = signal(false);
  readonly id = `info-tooltip-${nextId++}`;

  private host = inject(ElementRef<HTMLElement>);

  toggle() {
    this.open.update(open => !open);
  }

  close() {
    this.open.set(false);
  }

  onDocumentClick(event: MouseEvent) {
    if (this.open() && !this.host.nativeElement.contains(event.target as Node)) this.close();
  }
}
