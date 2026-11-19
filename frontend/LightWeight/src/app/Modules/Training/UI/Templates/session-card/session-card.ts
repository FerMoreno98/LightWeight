import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, input, output, signal } from '@angular/core';
import { RouterLink, UrlTree } from '@angular/router';
import { SeriesPerGroupPerSession } from '../../../data/training-api.service';
import { VolumeBars, volumeBars } from '../../shared/volume-bars/volume-bars';

/**
 * Card of a session of a template: its position, its total sets and the sets per muscle group as bars.
 * Same family as the template card, without the volume level (that belongs to the template).
 * The whole card opens the session; deleting lives in the ⋯ menu and the parent confirms it
 */
@Component({
  selector: 'app-session-card',
  standalone: true,
  imports: [RouterLink, VolumeBars],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block',
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'menuOpen.set(false)',
  },
  templateUrl: './session-card.html',
})
export class SessionCard {
  session = input.required<SeriesPerGroupPerSession>();
  /** Position of the session inside the template (1, 2...) */
  position = input.required<number>();
  /** Where the card goes: the sets of the session */
  link = input.required<UrlTree>();

  remove = output<void>();

  private host = inject(ElementRef<HTMLElement>);

  totalSets = computed(() => volumeBars(this.session().series).reduce((sum, bar) => sum + bar.sets, 0));

  menuOpen = signal(false);

  toggleMenu() {
    this.menuOpen.update(open => !open);
  }

  onDocumentClick(event: MouseEvent) {
    if (!event.composedPath().includes(this.host.nativeElement)) this.menuOpen.set(false);
  }

  onRemove() {
    this.menuOpen.set(false);
    this.remove.emit();
  }
}
