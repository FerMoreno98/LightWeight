import { AdvancedTechnique, TrainingDistribution, TrainingStage, VolumeLandmark } from '../../data/training-api.service';

export const TRAINING_STAGE_LABELS: Record<TrainingStage, string> = {
  Bulk: 'Volumen',
  Cut: 'Definición',
  Maintenance: 'Mantenimiento',
};

export const VOLUME_LANDMARK_LABELS: Record<VolumeLandmark, string> = {
  MV: 'MV — Maintenance Volume',
  MEV: 'MEV — Minimum Effective Volume',
  MAV: 'MAV — Maximum Adaptive Volume',
  MRV: 'MRV — Maximum Recoverable Volume',
};

export const TRAINING_DISTRIBUTION_LABELS: Record<TrainingDistribution, string> = {
  PushPullLegs: 'Push Pull Legs',
  UpperLower: 'Upper Lower',
  Weider: 'Weider',
  Phat: 'PHAT',
  FullBody: 'Full Body',
  Other: 'Otra',
};

export const TECHNIQUE_LABELS: Record<AdvancedTechnique, string> = {
  DropSet: 'Drop set',
  Cluster: 'Cluster',
  MyoRep: 'Myo-reps',
};

const DATE_FORMAT = new Intl.DateTimeFormat('es-ES', { day: 'numeric', month: 'short', year: 'numeric' });

/** Formats an ISO date returned by the API as "9 oct 2026" */
export function formatCycleDate(isoDate: string): string {
  return DATE_FORMAT.format(new Date(isoDate));
}

/** "9 oct 2026 – 12 nov 2026", or "Desde 9 oct 2026" while the cycle is active */
export function formatCyclePeriod(startedAt: string, finishedAt: string | null): string {
  return finishedAt
    ? `${formatCycleDate(startedAt)} – ${formatCycleDate(finishedAt)}`
    : `Desde ${formatCycleDate(startedAt)}`;
}
