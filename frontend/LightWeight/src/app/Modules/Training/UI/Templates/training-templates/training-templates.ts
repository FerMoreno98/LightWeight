import { Component, ElementRef, computed, effect, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { TrainingTabs } from '../../shared/training-tabs/training-tabs';
import { muscleGroupLabel } from '../../shared/muscle-groups';
import { Program, TrainingTemplate } from '../../../data/training-api.service';
import { PERIODIZATION_LABELS, TRAINING_DISTRIBUTION_LABELS, VOLUME_LANDMARK_LABELS } from '../../shared/program-labels';
import { ConfirmDialog } from '../../../../../Shared/confirm-dialog/confirm-dialog';
import { InfoTooltip } from '../../../../../Shared/info-tooltip/info-tooltip';

/** Explanation of the "Copiado de" subtitle: the name is read live, the content is a snapshot */
export const COPIED_FROM_INFO =
  'Es el nombre actual del autor: si lo cambia en su perfil, aquí se actualiza. ' +
  'El contenido del programa es tu copia y no cambia.';

export interface ProgramWithTemplates {
  program: Program;
  templates: TrainingTemplate[];
}

@Component({
  selector: 'app-training-templates',
  standalone: true,
  imports: [RouterLink, FormsModule, TrainingTabs, ConfirmDialog, InfoTooltip],
  templateUrl: './training-templates.html',
  styleUrl: './training-templates.css',
})
export class TrainingTemplates {
  private store = inject(TrainingStore);
  private router = inject(Router);

  // Local flag: two requests run in parallel and each one toggles the store flag
  private _isLoadingPage = signal(true);
  isLoading = this._isLoadingPage.asReadonly();
  error = this.store.error;

  programsWithTemplates = computed<ProgramWithTemplates[]>(() =>
    this.store.programs().map(program => ({
      program,
      templates: this.store.trainingTemplates()
        .filter(t => t.programId === program.id)
        .sort((a, b) => a.order - b.order),
    }))
  );

  async ngOnInit() {
    this._isLoadingPage.set(true);
    try {
      await Promise.all([
        this.store.GetUserPrograms(),
        this.store.GetUserTrainingTemplates(),
      ]);
    } finally {
      this._isLoadingPage.set(false);
    }
  }

  periodizationLabel(program: Program): string {
    return PERIODIZATION_LABELS[program.periodization] ?? program.periodization;
  }

  aimMuscleGroupLabels(program: Program): string[] {
    return program.aimMuscleGroups.map(group => muscleGroupLabel(group));
  }

  volumeLandmarkLabel(template: TrainingTemplate): string {
    return VOLUME_LANDMARK_LABELS[template.volumeLandmark] ?? template.volumeLandmark;
  }

  trainingDistributionLabel(template: TrainingTemplate): string {
    return TRAINING_DISTRIBUTION_LABELS[template.trainingDistribution] ?? template.trainingDistribution;
  }

  totalVolumeEntries(template: TrainingTemplate): [string, number][] {
    return Object.entries(template.totalVolume).map(([group, count]) => [muscleGroupLabel(group), count]);
  }

  goToSessions(template: TrainingTemplate) {
    this.router.navigate(['/training/sessiontemplate', template.id]);
  }

  async deleteTemplate(template: TrainingTemplate) {
    await this.store.DeleteTrainingTemplate(template.id);
  }

  /** Id of the template whose name is being edited (only one at a time) */
  editingNameId = signal<string | null>(null);
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

  startRename(template: TrainingTemplate) {
    this.editingNameId.set(template.id);
    this.editedName = template.name;
  }

  cancelRename() {
    this.editingNameId.set(null);
  }

  async saveRename(template: TrainingTemplate) {
    // Blur also fires after Enter/Escape: ignore it if the edit was already closed
    if (this.editingNameId() !== template.id) return;
    const name = this.editedName.trim();
    this.editingNameId.set(null);
    if (!name || name === template.name) return;
    await this.store.RenameTrainingTemplate(template.id, name);
  }

  // ---------------------------------------------------------------- Sharing

  readonly copiedFromInfo = COPIED_FROM_INFO;

  /** Id of the program whose link is being created/shared, to disable its button */
  sharingId = signal<string | null>(null);
  /** Id of the program whose link was just copied, to show "Enlace copiado" for a moment */
  linkCopiedId = signal<string | null>(null);
  private linkCopiedTimer: ReturnType<typeof setTimeout> | undefined;
  /** Link shown in a field when the clipboard is not available, so it can be copied by hand */
  manualLink = signal<{ programId: string; url: string } | null>(null);

  /** Program waiting for the confirmation to stop sharing it */
  revokeTarget = signal<Program | null>(null);
  isRevoking = signal(false);

  shareUrl(token: string): string {
    return `${location.origin}/share/${token}`;
  }

  /**
   * Creates the link if needed and hands it over: the native share sheet on mobile, the clipboard otherwise.
   * The token is the same every time until the program stops being shared
   */
  async shareProgram(program: Program) {
    if (this.sharingId()) return;
    this.sharingId.set(program.id);
    try {
      const token = program.shareToken ?? await this.store.ShareProgram(program.id);
      if (!token) return;
      const url = this.shareUrl(token);
      if (await this.nativeShare(program.name, url)) return;
      await this.copyLink(program.id, url);
    } finally {
      this.sharingId.set(null);
    }
  }

  /** Copies the current link of a shared program to the clipboard */
  async copyProgramLink(program: Program) {
    if (program.shareToken) await this.copyLink(program.id, this.shareUrl(program.shareToken));
  }

  /** True if the native share sheet handled it (or the user closed it on purpose) */
  private async nativeShare(name: string, url: string): Promise<boolean> {
    // Only touch devices: on desktop the clipboard is more direct than the system dialog
    if (!navigator.share || !matchMedia('(pointer: coarse)').matches) return false;
    try {
      await navigator.share({ title: name, text: `Mi programa de entrenamiento "${name}" en LightWeight`, url });
      return true;
    } catch (error) {
      return error instanceof DOMException && error.name === 'AbortError';
    }
  }

  private async copyLink(programId: string, url: string) {
    try {
      await navigator.clipboard.writeText(url);
      this.manualLink.set(null);
      this.linkCopiedId.set(programId);
      clearTimeout(this.linkCopiedTimer);
      this.linkCopiedTimer = setTimeout(() => this.linkCopiedId.set(null), 2500);
    } catch {
      // Without clipboard permission (or http) the link is still useful: show it so it can be copied by hand
      this.manualLink.set({ programId, url });
    }
  }

  askRevoke(program: Program) {
    this.revokeTarget.set(program);
  }

  cancelRevoke() {
    this.revokeTarget.set(null);
  }

  async confirmRevoke() {
    const program = this.revokeTarget();
    if (!program) return;
    this.isRevoking.set(true);
    try {
      if (await this.store.RevokeProgramShare(program.id) && this.manualLink()?.programId === program.id)
        this.manualLink.set(null);
    } finally {
      this.isRevoking.set(false);
      this.revokeTarget.set(null);
    }
  }

  ngOnDestroy() {
    clearTimeout(this.linkCopiedTimer);
  }

  /** Id of the template being duplicated, to disable its button and avoid double copies */
  duplicatingId = signal<string | null>(null);

  async duplicateTemplate(template: TrainingTemplate) {
    if (this.duplicatingId()) return;
    this.duplicatingId.set(template.id);
    try {
      await this.store.DuplicateTrainingTemplate(template.id);
    } finally {
      this.duplicatingId.set(null);
    }
  }
}
