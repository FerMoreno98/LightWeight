import { Component, ElementRef, computed, effect, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { MuscleGroup, Periodization, Program, TrainingDistribution, TrainingTemplate, VolumeLandmark } from '../../../data/training-api.service';

const VOLUME_LANDMARK_LABELS: Record<VolumeLandmark, string> = {
  MV: 'MV — Maintenance Volume',
  MEV: 'MEV — Minimum Effective Volume',
  MAV: 'MAV — Maximum Adaptive Volume',
  MRV: 'MRV — Maximum Recoverable Volume',
};

const TRAINING_DISTRIBUTION_LABELS: Record<TrainingDistribution, string> = {
  PushPullLegs: 'Push Pull Legs',
  UpperLower: 'Upper Lower',
  Weider: 'Weider',
  Phat: 'Phat',
  FullBody: 'Full Body',
  Other: 'Otra',
};

const PERIODIZATION_LABELS: Record<Periodization, string> = {
  Linear: 'Lineal',
  Ondulating: 'Ondulante',
  block: 'Bloques',
  MikeIsraetel: 'Mike Israetel',
};

const MUSCLE_GROUP_LABELS: Record<MuscleGroup, string> = {
  Shoulder: 'Hombro',
  Back: 'Espalda',
  Chest: 'Pecho',
  Biceps: 'Bíceps',
  Triceps: 'Tríceps',
  Glutes: 'Glúteos',
  Quads: 'Cuádriceps',
  Hamstring: 'Isquios',
  Calves: 'Gemelos',
};

export interface ProgramWithTemplates {
  program: Program;
  templates: TrainingTemplate[];
}

@Component({
  selector: 'app-training-templates',
  standalone: true,
  imports: [RouterLink, FormsModule],
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
    return program.aimMuscleGroups.map(group => MUSCLE_GROUP_LABELS[group] ?? group);
  }

  volumeLandmarkLabel(template: TrainingTemplate): string {
    return VOLUME_LANDMARK_LABELS[template.volumeLandmark] ?? template.volumeLandmark;
  }

  trainingDistributionLabel(template: TrainingTemplate): string {
    return TRAINING_DISTRIBUTION_LABELS[template.trainingDistribution] ?? template.trainingDistribution;
  }

  totalVolumeEntries(template: TrainingTemplate): [string, number][] {
    return Object.entries(template.totalVolume).map(([group, count]) => [MUSCLE_GROUP_LABELS[group as MuscleGroup] ?? group, count]);
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
