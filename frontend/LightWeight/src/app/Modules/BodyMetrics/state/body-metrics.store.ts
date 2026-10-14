import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { BodyMetricsApiService, MeasurementEntry, MeasurementKind, MeasureValues } from '../data/body-metrics-api.service';

@Injectable({ providedIn: 'root' })
export class BodyMetricsStore {
    private api = inject(BodyMetricsApiService);

    private _history = signal<Record<MeasurementKind, MeasurementEntry[]>>({ weights: [], perimeters: [], skinfolds: [] });
    private _loaded = signal<Record<MeasurementKind, boolean>>({ weights: false, perimeters: false, skinfolds: false });
    private _error = signal<string | null>(null);

    /** Latest measurements of each kind, from the most recent */
    history = this._history.asReadonly();
    /** Whether the history of each kind has been loaded at least once */
    loaded = this._loaded.asReadonly();
    error = this._error.asReadonly();

    async GetHistory(kind: MeasurementKind): Promise<boolean> {
        this._error.set(null);
        try {
            const entries = await firstValueFrom(this.api.GetHistory(kind));
            this._history.update(h => ({ ...h, [kind]: entries }));
            this._loaded.update(l => ({ ...l, [kind]: true }));
            return true;
        } catch {
            this._error.set('No se ha podido cargar el historial');
            return false;
        }
    }

    /** Registers a measurement and reloads the history */
    async Register(kind: MeasurementKind, values: MeasureValues, measuredAt: string): Promise<boolean> {
        this._error.set(null);
        try {
            await firstValueFrom(this.api.Register(kind, values, measuredAt));
        } catch {
            this._error.set('No se ha podido registrar la medida. Revisa que los valores estén dentro de lo razonable y que la fecha no sea futura');
            return false;
        }
        return this.GetHistory(kind);
    }

    /** Corrects a measurement and reloads the history (its new date may move it out of the latest ones) */
    async Correct(kind: MeasurementKind, measurementId: string, values: MeasureValues, measuredAt: string): Promise<boolean> {
        this._error.set(null);
        try {
            await firstValueFrom(this.api.Correct(kind, measurementId, values, measuredAt));
        } catch {
            this._error.set('No se ha podido corregir la medida');
            return false;
        }
        return this.GetHistory(kind);
    }

    /** Deletes a measurement and reloads the history, so the next older one takes its place */
    async Delete(kind: MeasurementKind, measurementId: string): Promise<boolean> {
        this._error.set(null);
        try {
            await firstValueFrom(this.api.Delete(kind, measurementId));
        } catch {
            this._error.set('No se ha podido borrar la medida');
            return false;
        }
        return this.GetHistory(kind);
    }

    clearError() {
        this._error.set(null);
    }
}
