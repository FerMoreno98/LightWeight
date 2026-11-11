import { Periodization, TrainingDistribution, VolumeLandmark } from '../../data/training-api.service';

/** Labels of the program and template enums, shared by the programs list and the shared program preview */

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
  Phat: 'Phat',
  FullBody: 'Full Body',
  Other: 'Otra',
};

export const PERIODIZATION_LABELS: Record<Periodization, string> = {
  Linear: 'Lineal',
  Ondulating: 'Ondulante',
  block: 'Bloques',
  MikeIsraetel: 'Mike Israetel',
};
