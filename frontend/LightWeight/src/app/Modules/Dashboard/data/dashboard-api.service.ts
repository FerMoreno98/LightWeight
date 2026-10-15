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

    /** Empties the projections of the user and fills them again from the source modules */
    Rebuild(): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/rebuild`, null);
    }
}
