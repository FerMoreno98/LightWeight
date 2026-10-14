import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../Environments/Environment';

/** Kinds of measurement, as they appear in the API routes */
export type MeasurementKind = 'weights' | 'perimeters' | 'skinfolds';

/** Values of a measurement by field (missing ones are null) */
export type MeasureValues = Record<string, number | null>;

/** A measurement as returned by the history endpoints: id, date and its values */
export interface MeasurementEntry {
    id: string;
    /** ISO date in UTC */
    measuredAt: string;
    /** Sum of the six skinfolds, only in skinfolds and only when all of them were measured */
    summatory?: number | null;
    [field: string]: number | string | null | undefined;
}

@Service()
export class BodyMetricsApiService {
    private http = inject(HttpClient);
    private baseUrl = `${environment.apiUrl}/bodymetrics`;

    /** The latest measurements of the user (the backend returns the last 10), from the most recent */
    GetHistory(kind: MeasurementKind): Observable<MeasurementEntry[]> {
        return this.http.get<MeasurementEntry[]>(`${this.baseUrl}/${kind}`);
    }

    /** The same body is used to register and to correct: { measuredAt, ...values } */
    Register(kind: MeasurementKind, values: MeasureValues, measuredAt: string): Observable<{ id: string }> {
        return this.http.post<{ id: string }>(`${this.baseUrl}/${kind}`, { ...values, measuredAt });
    }

    /** Replaces the values and the date of a measurement (fields not sent become null) */
    Correct(kind: MeasurementKind, measurementId: string, values: MeasureValues, measuredAt: string): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/${kind}/${measurementId}`, { ...values, measuredAt });
    }

    Delete(kind: MeasurementKind, measurementId: string): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${kind}/${measurementId}`);
    }
}
