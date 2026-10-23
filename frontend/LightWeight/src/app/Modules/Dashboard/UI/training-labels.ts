import { RepZone } from '../data/dashboard-api.service';

/**
 * Spanish names of the muscle groups. The keys come from the enum of training (its last value is "calves" in
 * lowercase), so they are looked up ignoring the case
 */
const MUSCLE_LABELS: Record<string, string> = {
    shoulder: 'Hombro',
    back: 'Espalda',
    chest: 'Pecho',
    biceps: 'Bíceps',
    triceps: 'Tríceps',
    glutes: 'Glúteos',
    quads: 'Cuádriceps',
    hamstring: 'Isquios',
    calves: 'Gemelos',
};

/** Order in which the muscle groups are listed: the one of the enum of training */
export const MUSCLE_ORDER = Object.keys(MUSCLE_LABELS);

export function muscleLabel(muscle: string): string {
    return MUSCLE_LABELS[muscle.toLowerCase()] ?? muscle;
}

export const ZONE_LABELS: Record<RepZone, string> = {
    Strength: 'Fuerza (≤ 6)',
    Hypertrophy: 'Hipertrofia (7–15)',
    Endurance: 'Resistencia (≥ 16)',
};

/** Zones in a fixed order, so each one always takes the same color */
export const ZONE_ORDER: RepZone[] = ['Strength', 'Hypertrophy', 'Endurance'];

const TECHNIQUE_LABELS: Record<string, string> = {
    DropSet: 'drop set',
    Cluster: 'cluster',
    MyoRep: 'myo-reps',
};

export function techniqueLabel(technique: string | null): string | null {
    return technique ? (TECHNIQUE_LABELS[technique] ?? technique) : null;
}

/** "sueño 4 · agujetas 8 · motivación 5": the wellbeing a session started with */
export function wellbeingLine(sleep: number, doms: number, motivation: number): string {
    return `sueño ${sleep} · agujetas ${doms} · motivación ${motivation}`;
}
