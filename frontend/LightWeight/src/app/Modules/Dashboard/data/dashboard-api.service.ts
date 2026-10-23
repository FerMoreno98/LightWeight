import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../Environments/Environment';

/** A body weight (kg) */
export interface WeightPoint {
    /** ISO date in UTC */
    measuredAt: string;
    weight: number;
}

/** Average of the weights of a week (Monday to Sunday, UTC) */
export interface WeeklyAverage {
    /** Monday 00:00 UTC, ISO */
    weekStart: string;
    average: number;
    /** Weights the average is made of */
    count: number;
}

export interface WeightEvolution {
    /** Every weight of the user, from the oldest */
    points: WeightPoint[];
    weeklyAverages: WeeklyAverage[];
}

/** A measurement of perimeters (cm) or skinfolds (mm): the date and one value per field, null if not measured */
export interface EvolutionPoint {
    /** ISO date in UTC */
    measuredAt: string;
    [field: string]: number | string | null;
}

// ---------------------------------------------------------------- Training

/** Sets per muscle group of a training week (keys: muscle group names of training, e.g. "Chest") */
export interface WeeklyVolume {
    microcycleId: string;
    weekNumber: number;
    /** Date of the first session of the week, ISO */
    startedAt: string;
    setsByMuscle: Record<string, number>;
}

/** A session with its performance against the previous time and the wellbeing it started with (1-10) */
export interface SessionPerformance {
    sessionId: string;
    name: string;
    startedAt: string;
    /** Average change in % against the previous time of its exercises; null if none could be compared */
    performance: number | null;
    comparedExercises: number;
    motivation: number;
    sleep: number;
    doms: number;
    /** Exercises left out of the average because their change is not plausible (over ±50 %): worth checking */
    excluded: { exerciseName: string; change: number }[];
}

export type RepZone = 'Strength' | 'Hypertrophy' | 'Endurance';

/** An exercise of the user, for the list of Progreso */
export interface ExerciseSummary {
    exerciseId: string;
    name: string;
    lastPerformedAt: string;
    sessions: number;
    /** Best estimated 1RM of the last session that has one; null for body weight exercises */
    lastEstimatedOneRepMax: number | null;
    /** Change in % against the previous session in the same zone; null if there is none */
    trend: number | null;
}

export interface ExerciseProgress {
    exerciseId: string;
    name: string;
    progression: {
        performedAt: string;
        sessionName: string;
        zone: RepZone;
        estimatedOneRepMax: number | null;
        bestWeight: number | null;
        /** The 1RM comes from a long set (reps + RIR over 15) */
        lowPrecision: boolean;
        motivation: number;
        sleep: number;
        doms: number;
    }[];
    fatigue: { performedAt: string; zone: RepZone; firstToSecond: number; firstToLast: number }[];
    recentSessions: {
        performedAt: string;
        sessionName: string;
        zone: RepZone;
        sets: { order: number; weight: number; repetitions: number; rpe: number; isBodyWeight: boolean; technique: string | null }[];
    }[];
}

/** Read side of the dashboard: projections built from the events of the other modules */
@Service()
export class DashboardApiService {
    private http = inject(HttpClient);
    private baseUrl = `${environment.apiUrl}/dashboard`;

    GetWeightEvolution(): Observable<WeightEvolution> {
        return this.http.get<WeightEvolution>(`${this.baseUrl}/bodymetrics/weight`);
    }

    /** Every measurement of perimeters, from the oldest */
    GetPerimetersEvolution(): Observable<EvolutionPoint[]> {
        return this.http.get<EvolutionPoint[]>(`${this.baseUrl}/bodymetrics/perimeters`);
    }

    /** Every measurement of skinfolds, from the oldest, with the summatory when the six were measured */
    GetSkinFoldsEvolution(): Observable<EvolutionPoint[]> {
        return this.http.get<EvolutionPoint[]>(`${this.baseUrl}/bodymetrics/skinfolds`);
    }

    GetTrainingVolume(): Observable<WeeklyVolume[]> {
        return this.http.get<WeeklyVolume[]>(`${this.baseUrl}/training/volume`);
    }

    GetSessionsPerformance(): Observable<SessionPerformance[]> {
        return this.http.get<SessionPerformance[]>(`${this.baseUrl}/training/sessions`);
    }

    /** The exercises the user has performed, from the most recently trained */
    GetExercises(): Observable<ExerciseSummary[]> {
        return this.http.get<ExerciseSummary[]>(`${this.baseUrl}/training/exercises`);
    }

    /** Whole history of one exercise; 404 if the user never performed it */
    GetExerciseProgress(exerciseId: string): Observable<ExerciseProgress> {
        return this.http.get<ExerciseProgress>(`${this.baseUrl}/training/exercises/${exerciseId}`);
    }

    /** Empties the projections of the user and fills them again from the source modules */
    Rebuild(): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/rebuild`, null);
    }
}
