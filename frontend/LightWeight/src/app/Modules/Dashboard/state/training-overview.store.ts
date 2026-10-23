import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DashboardApiService, SessionPerformance, WeeklyVolume } from '../data/dashboard-api.service';

/** Data of the training cards of the home: weekly volume and session performance with wellbeing */
@Injectable({ providedIn: 'root' })
export class TrainingOverviewStore {
    private api = inject(DashboardApiService);

    private _volume = signal<WeeklyVolume[]>([]);
    private _sessions = signal<SessionPerformance[]>([]);
    private _loading = signal(false);
    private _loaded = signal(false);
    private _rebuilding = signal(false);
    private _error = signal<string | null>(null);

    volume = this._volume.asReadonly();
    sessions = this._sessions.asReadonly();
    loading = this._loading.asReadonly();
    /** Whether the data has been loaded at least once (to tell "loading" from "empty") */
    loaded = this._loaded.asReadonly();
    rebuilding = this._rebuilding.asReadonly();
    error = this._error.asReadonly();

    async Load(): Promise<boolean> {
        this._error.set(null);
        this._loading.set(true);
        try {
            const [volume, sessions] = await Promise.all([
                firstValueFrom(this.api.GetTrainingVolume()),
                firstValueFrom(this.api.GetSessionsPerformance()),
            ]);
            this._volume.set(volume);
            this._sessions.set(sessions);
            this._loaded.set(true);
            return true;
        } catch {
            this._error.set('No se ha podido cargar la evolución de tus entrenamientos');
            return false;
        } finally {
            this._loading.set(false);
        }
    }

    /** Rebuilds the projections from the source modules (the first time, it loads the whole history) and reloads */
    async Rebuild(): Promise<boolean> {
        this._error.set(null);
        this._rebuilding.set(true);
        try {
            await firstValueFrom(this.api.Rebuild());
        } catch {
            this._error.set('No se han podido recalcular las gráficas');
            return false;
        } finally {
            this._rebuilding.set(false);
        }
        return this.Load();
    }
}
