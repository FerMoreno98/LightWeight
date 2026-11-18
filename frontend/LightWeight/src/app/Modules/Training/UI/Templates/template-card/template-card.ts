import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TrainingTemplate, VolumeLandmark } from '../../../data/training-api.service';
import { TRAINING_DISTRIBUTION_LABELS, VOLUME_LANDMARK_LABELS } from '../../shared/program-labels';
import { LANDMARK_TONES, VOLUME_LANDMARK_ORDER, VolumeGauge } from '../../shared/volume-gauge/volume-gauge';
import { VolumeBars, volumeBars } from '../../shared/volume-bars/volume-bars';

/**
 * Card of a training template: its volume level as a gauge, the weekly sets and the groups with most sets.
 * The whole card opens the template; renaming, changing the level, duplicating and deleting live in the ⋯ menu.
 * It only edits locally: the parent saves (and confirms the delete)
 */
@Component({
  selector: 'app-template-card',
  standalone: true,
  imports: [FormsModule, RouterLink, VolumeGauge, VolumeBars],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block',
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'closeOverlays()',
  },
  templateUrl: './template-card.html',
})
export class TemplateCard {
  template = input.required<TrainingTemplate>();
  /** This template is being duplicated */
  busy = input(false);
  /** Another duplicate is running: no new one can start */
  duplicateDisabled = input(false);

  rename = output<string>();
  landmarkChange = output<VolumeLandmark>();
  duplicate = output<void>();
  remove = output<void>();

  private host = inject(ElementRef<HTMLElement>);

  readonly landmarks = VOLUME_LANDMARK_ORDER;
  readonly landmarkLabels = VOLUME_LANDMARK_LABELS;

  tone = computed(() => LANDMARK_TONES[this.template().volumeLandmark]);
  distribution = computed(() => {
    const distribution = this.template().trainingDistribution;
    return TRAINING_DISTRIBUTION_LABELS[distribution] ?? distribution;
  });

  totalSets = computed(() => volumeBars(this.template().totalVolume).reduce((sum, bar) => sum + bar.sets, 0));
  totalLabel = computed(() => {
    const days = this.template().durationInDays;
    return days === 7 ? 'series / semana' : `series en ${days} días`;
  });

  // ---------------------------------------------------------------- Menu

  menuOpen = signal(false);

  toggleMenu() {
    this.menuOpen.update(open => !open);
  }

  closeOverlays() {
    this.menuOpen.set(false);
    this.editingLandmark.set(false);
  }

  /**
   * Closes the menu and the level picker when clicking outside the card. Uses the event path, not contains():
   * the menu item that was clicked ("Cambiar nivel") disappears once the view updates, and the path keeps it either way
   */
  onDocumentClick(event: MouseEvent) {
    if (!event.composedPath().includes(this.host.nativeElement)) this.closeOverlays();
  }

  onDuplicate() {
    this.menuOpen.set(false);
    this.duplicate.emit();
  }

  onRemove() {
    this.menuOpen.set(false);
    this.remove.emit();
  }

  // ---------------------------------------------------------------- Rename

  renaming = signal(false);
  editedName = '';
  private nameInput = viewChild<ElementRef<HTMLInputElement>>('nameInput');

  constructor() {
    // Focus and select the name as soon as the input appears
    effect(() => {
      const input = this.nameInput()?.nativeElement;
      input?.focus();
      input?.select();
    });
  }

  startRename() {
    this.menuOpen.set(false);
    this.editedName = this.template().name;
    this.renaming.set(true);
  }

  cancelRename() {
    this.renaming.set(false);
  }

  saveRename() {
    // Blur also fires after Enter/Escape: ignore it if the edit was already closed
    if (!this.renaming()) return;
    this.renaming.set(false);
    const name = this.editedName.trim();
    if (name && name !== this.template().name) this.rename.emit(name);
  }

  // ---------------------------------------------------------------- Volume level

  editingLandmark = signal(false);

  startLandmarkEdit() {
    this.menuOpen.set(false);
    this.editingLandmark.set(true);
  }

  chooseLandmark(landmark: VolumeLandmark) {
    this.editingLandmark.set(false);
    if (landmark !== this.template().volumeLandmark) this.landmarkChange.emit(landmark);
  }

  landmarkButtonClass(landmark: VolumeLandmark): string {
    const tone = LANDMARK_TONES[landmark];
    return landmark === this.template().volumeLandmark
      ? `${tone.border} ${tone.text} ${tone.soft}`
      : 'border-line-strong text-muted hover:bg-raised hover:text-fg';
  }
}
