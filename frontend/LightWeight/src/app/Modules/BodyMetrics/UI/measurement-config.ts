import { MeasurementKind } from '../data/body-metrics-api.service';

/** One value of a measurement. The key is the field name in the API */
export interface MeasureField {
  key: string;
  label: string;
  /** Section of the form where it is shown (e.g. "Brazos") */
  group?: string;
}

/**
 * Describes a kind of measurement so a single panel can register, list, correct and delete it.
 * The limits mirror the domain rules of the backend, to warn before sending
 */
export interface MeasurementConfig {
  kind: MeasurementKind;
  title: string;
  description: string;
  unit: string;
  /** Lowest value accepted */
  min: number;
  /** Whether min itself is accepted (weight) or the value must be greater (perimeters and skinfolds) */
  minInclusive: boolean;
  max: number;
  step: number;
  fields: MeasureField[];
  /** Extra value shown in the history (the summatory of the skinfolds) */
  summary?: { key: string; label: string; hint: string };
}

export const WEIGHT_CONFIG: MeasurementConfig = {
  kind: 'weights',
  title: 'Peso',
  description: 'Tu peso corporal. Lo ideal es pesarte siempre en las mismas condiciones, por ejemplo en ayunas.',
  unit: 'kg',
  min: 20,
  minInclusive: true,
  max: 400,
  step: 0.1,
  fields: [{ key: 'weight', label: 'Peso' }],
};

export const PERIMETERS_CONFIG: MeasurementConfig = {
  kind: 'perimeters',
  title: 'Perímetros',
  description: 'Rellena solo los que hayas medido. Al corregir una medición se sustituye entera.',
  unit: 'cm',
  min: 0,
  minInclusive: false,
  max: 300,
  step: 0.1,
  fields: [
    { key: 'neck', label: 'Cuello', group: 'Tronco' },
    { key: 'shoulder', label: 'Hombros', group: 'Tronco' },
    { key: 'chest', label: 'Pecho', group: 'Tronco' },
    { key: 'waist', label: 'Cintura', group: 'Tronco' },
    { key: 'abs', label: 'Abdomen', group: 'Tronco' },
    { key: 'hip', label: 'Cadera', group: 'Tronco' },
    { key: 'rightArmRelaxed', label: 'Brazo dcho. relajado', group: 'Brazos' },
    { key: 'rightArmTensioned', label: 'Brazo dcho. contraído', group: 'Brazos' },
    { key: 'leftArmRelaxed', label: 'Brazo izdo. relajado', group: 'Brazos' },
    { key: 'leftArmTensioned', label: 'Brazo izdo. contraído', group: 'Brazos' },
    { key: 'rightThigh', label: 'Muslo dcho.', group: 'Piernas' },
    { key: 'leftThigh', label: 'Muslo izdo.', group: 'Piernas' },
    { key: 'rightCalf', label: 'Gemelo dcho.', group: 'Piernas' },
    { key: 'leftCalf', label: 'Gemelo izdo.', group: 'Piernas' },
  ],
};

export const SKINFOLDS_CONFIG: MeasurementConfig = {
  kind: 'skinfolds',
  title: 'Pliegues',
  description: 'Medidos con plicómetro. Con los seis se calcula el sumatorio.',
  unit: 'mm',
  min: 0,
  minInclusive: false,
  max: 100,
  step: 0.1,
  fields: [
    { key: 'abdominal', label: 'Abdominal' },
    { key: 'suprailiac', label: 'Suprailíaco' },
    { key: 'tricipital', label: 'Tricipital' },
    { key: 'subscapular', label: 'Subescapular' },
    { key: 'thigh', label: 'Muslo' },
    { key: 'calf', label: 'Gemelo' },
  ],
  summary: { key: 'summatory', label: 'Sumatorio', hint: 'Solo con los seis pliegues' },
};

export const MEASUREMENT_CONFIGS: MeasurementConfig[] = [WEIGHT_CONFIG, PERIMETERS_CONFIG, SKINFOLDS_CONFIG];
