import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DashboardApiService, ExerciseProgress, ExerciseSummary } from '../data/dashboard-api.service';
import { TrainingApiService } from '../../Training/data/training-api.service';

/** What a filter of Progreso covers: the current macrocycle or the whole history */
export type ProgressScope = 'macrocycle' | 'history';

/**
 * Data of the Progreso section: the list of exercises and the detail of the one opened. Two independent filters:
 * which exercises are listed and which sessions the marks (sessions, 1RM, trend and the detail) are computed with.
 * Both fall back to the whole history when there is no active macrocycle. Kept while the app is open
 */
@Injectable({ providedIn: 'root' })
export class ExerciseProgressStore {
    private api = inject(DashboardApiService);
    private trainingApi = inject(TrainingApiService);

    private _exercises = signal<ExerciseSummary[]>([]);
    private _listLoaded = signal(false);
    private _detail = signal<ExerciseProgress | null>(null);
    private _loading = signal(false);
    private _notFound = signal(false);
    private _error = signal<string | null>(null);
    /** Active macrocycle of the user; null when there is none (or it could not be read) */
    private _macrocycleId = signal<string | null>(null);
    private _exercisesScope = signal<ProgressScope>('macrocycle');
    private _marksScope = signal<ProgressScope>('macrocycle');
    /** Only the answer of the last request is shown, so changing a filter quickly never leaves stale data */
    private listRequest = 0;
    private detailRequest = 0;

    exercises = this._exercises.asReadonly();
    listLoaded = this._listLoaded.asReadonly();
    detail = this._detail.asReadonly();
    loading = this._loading.asReadonly();
    /** The opened exercise has never been performed by the user */
    notFound = this._notFound.asReadonly();
    error = this._error.asReadonly();
    hasMacrocycle = computed(() => this._macrocycleId() !== null);
    /** The scope chosen; without an active macrocycle it is always the history */
    exercisesScope = computed<ProgressScope>(() => this.hasMacrocycle() ? this._exercisesScope() : 'history');
    marksScope = computed<ProgressScope>(() => this.hasMacrocycle() ? this._marksScope() : 'history');

    private exercisesMacrocycleId = computed(() => this.exercisesScope() === 'macrocycle' ? this._macrocycleId() : null);
    private marksMacrocycleId = computed(() => this.marksScope() === 'macrocycle' ? this._macrocycleId() : null);

    SetExercisesScope(scope: ProgressScope) {
        if (scope === this.exercisesScope()) return;
        this._exercisesScope.set(scope);
        this.FetchExercises();
    }

    /** Reloads the opened exercise too, when given */
    SetMarksScope(scope: ProgressScope, openedExerciseId?: string) {
        if (scope === this.marksScope()) return;
        this._marksScope.set(scope);
        if (openedExerciseId) this.FetchExercise(openedExerciseId);
        else this.FetchExercises();
    }

    /** Reads the active macrocycle again (it may have changed since the last visit) and loads the list */
    async LoadExercises(): Promise<boolean> {
        await this.LoadMacrocycle();
        return this.FetchExercises();
    }

    async LoadExercise(exerciseId: string): Promise<boolean> {
        // Opened directly (link or reload): the macrocycle has not been read yet
        if (!this._listLoaded()) await this.LoadMacrocycle();
        return this.FetchExercise(exerciseId);
    }

    private async LoadMacrocycle() {
        try {
            const current = await firstValueFrom(this.trainingApi.GetCurrentMicrocycle());
            this._macrocycleId.set(current.macrocycleId);
        } catch {
            // Not critical: the filters show the whole history
            this._macrocycleId.set(null);
        }
    }

    private async FetchExercises(): Promise<boolean> {
        const request = ++this.listRequest;
        this._error.set(null);
        this._loading.set(true);
        try {
            const exercises = await firstValueFrom(this.api.GetExercises(this.exercisesMacrocycleId(), this.marksMacrocycleId()));
            if (request !== this.listRequest) return false;
            this._exercises.set(exercises);
            this._listLoaded.set(true);
            return true;
        } catch {
            if (request === this.listRequest) this._error.set('No se han podido cargar tus ejercicios');
            return false;
        } finally {
            if (request === this.listRequest) this._loading.set(false);
        }
    }

    private async FetchExercise(exerciseId: string): Promise<boolean> {
        const request = ++this.detailRequest;
        this._error.set(null);
        this._notFound.set(false);
        this._detail.set(null);
        this._loading.set(true);
        try {
            const detail = await firstValueFrom(this.api.GetExerciseProgress(exerciseId, this.marksMacrocycleId()));
            if (request !== this.detailRequest) return false;
            this._detail.set(detail);
            return true;
        } catch (error) {
            if (request !== this.detailRequest) return false;
            if (error instanceof HttpErrorResponse && error.status === 404) this._notFound.set(true);
            else this._error.set('No se ha podido cargar el progreso del ejercicio');
            return false;
        } finally {
            if (request === this.detailRequest) this._loading.set(false);
        }
    }
}
