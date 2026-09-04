import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { MuscleGroup, TrainingDistribution, TrainingTemplate, VolumeLandmark } from '../../../data/training-api.service';

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

@Component({
  selector: 'app-training-templates',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './training-templates.html',
  styleUrl: './training-templates.css',
})
export class TrainingTemplates {
  private store = inject(TrainingStore);
  private router = inject(Router);

  isLoading = this.store.isLoading;
  error = this.store.error;
  templates = this.store.trainingTemplates;

  async ngOnInit() {
    await this.store.GetUserTrainingTemplates();
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
}
