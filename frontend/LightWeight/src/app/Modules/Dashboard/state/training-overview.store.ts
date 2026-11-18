import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DashboardApiService, SessionPerformance, WeeklyVolume } from '../data/dashboard-api.service';
import { TrainingApiService } from '../../Training/data/training-api.service';
import { ProgressScope } from './exercise-progress.store';

/** Data of the training cards of the home: weekly volume and session performance with wellbeing */
@Injectable({ providedIn: 'root' })
export class TrainingOverviewStore {
    private api = inject(DashboardApiService);
    private trainingApi = inject(TrainingApiService);

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

    // ---------------------------------------------------------------- Period of the session performance

    /** Active macrocycle of the user; null when there is none (or it could not be read) */
    private _macrocycleId = signal<string | null>(null);
    private _performanceScope = signal<ProgressScope>('macrocycle');
    private sessionsRequest = 0;
    hasMacrocycle = computed(() => this._macrocycleId() !== null);
    /** The period chosen; without an active macrocycle it is always the history */
    performanceScope = computed<ProgressScope>(() => this.hasMacrocycle() ? this._performanceScope() : 'history');
    private performanceMacrocycleId = computed(() => this.performanceScope() === 'macrocycle' ? this._macrocycleId() : null);

    /** Changes the period of the performance chart and reloads only its sessions */
    async SetPerformanceScope(scope: ProgressScope): Promise<boolean> {
        if (scope === this.performanceScope()) return true;
        this._performanceScope.set(scope);
        const request = ++this.sessionsRequest;
        this._error.set(null);
        try {
            const sessions = await firstValueFrom(this.api.GetSessionsPerformance(this.performanceMacrocycleId()));
            if (request === this.sessionsRequest) this._sessions.set(sessions);
            return true;
        } catch {
            if (request === this.sessionsRequest) this._error.set('No se ha podido cargar el rendimiento de tus sesiones');
            return false;
        }
    }

    private async LoadMacrocycle() {
        try {
            const current = await firstValueFrom(this.trainingApi.GetCurrentMicrocycle());
            this._macrocycleId.set(current.macrocycleId);
        } catch {
            // Not critical: the chart shows the whole history
            this._macrocycleId.set(null);
        }
    }

    async Load(): Promise<boolean> {
        this._error.set(null);
        this._loading.set(true);
        try {
            // The active macrocycle may have changed since the last visit
            await this.LoadMacrocycle();
            const request = ++this.sessionsRequest;
            const [volume, sessions] = await Promise.all([
                firstValueFrom(this.api.GetTrainingVolume()),
                firstValueFrom(this.api.GetSessionsPerformance(this.performanceMacrocycleId())),
            ]);
            this._volume.set(volume);
            // The period may have been changed meanwhile: its own request brings the sessions
            if (request === this.sessionsRequest) this._sessions.set(sessions);
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
