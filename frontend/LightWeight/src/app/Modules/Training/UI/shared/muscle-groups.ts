import { Laterality, MuscleGroup } from '../../data/training-api.service';

/** Spanish names of the lateralities of an exercise */
export const LATERALITY_LABELS: Record<Laterality, string> = {
  Bilateral: 'Bilateral',
  Unilateral: 'Unilateral',
  Both: 'Bilateral o unilateral',
};

/** Spanish names of the muscle groups, in the order of the backend enum (its numeric value is the index) */
export const MUSCLE_GROUP_LABELS: Record<MuscleGroup, string> = {
  Chest: 'Pecho',
  Back: 'Espalda',
  Traps: 'Trapecio',
  FrontDelts: 'Deltoides anterior',
  SideDelts: 'Deltoides lateral',
  RearDelts: 'Deltoides posterior',
  Triceps: 'Tríceps',
  Biceps: 'Bíceps',
  Forearms: 'Antebrazo',
  Abs: 'Abdomen',
  Glutes: 'Glúteos',
  Quads: 'Cuádriceps',
  Hamstring: 'Isquios',
  Calves: 'Gemelos',
  Adductors: 'Aductores',
};

/** Muscle groups in enum order: `MUSCLE_GROUPS[n]` is the group whose numeric value is n */
export const MUSCLE_GROUPS = Object.keys(MUSCLE_GROUP_LABELS) as MuscleGroup[];

const LABELS_BY_LOWERCASE = new Map(MUSCLE_GROUPS.map(group => [group.toLowerCase(), MUSCLE_GROUP_LABELS[group]]));

/**
 * Spanish name of a muscle group, from its name ("Chest", ignoring case) or its numeric value (2).
 * Unknown values are shown as they come
 */
export function muscleGroupLabel(group: string | number): string {
  if (typeof group === 'number') return MUSCLE_GROUP_LABELS[MUSCLE_GROUPS[group]] ?? 'Otro';
  return LABELS_BY_LOWERCASE.get(group.toLowerCase()) ?? group;
}
