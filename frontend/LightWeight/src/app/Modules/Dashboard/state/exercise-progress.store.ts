import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DashboardApiService, ExerciseProgress, ExerciseSummary } from '../data/dashboard-api.service';

/** Data of the Progreso section: the list of exercises and the detail of the one opened */
@Injectable({ providedIn: 'root' })
export class ExerciseProgressStore {
    private api = inject(DashboardApiService);

    private _exercises = signal<ExerciseSummary[]>([]);
    private _listLoaded = signal(false);
    private _detail = signal<ExerciseProgress | null>(null);
    private _loading = signal(false);
    private _notFound = signal(false);
    private _error = signal<string | null>(null);

    exercises = this._exercises.asReadonly();
    listLoaded = this._listLoaded.asReadonly();
    detail = this._detail.asReadonly();
    loading = this._loading.asReadonly();
    /** The opened exercise has never been performed by the user */
    notFound = this._notFound.asReadonly();
    error = this._error.asReadonly();

    async LoadExercises(): Promise<boolean> {
        this._error.set(null);
        this._loading.set(true);
        try {
            this._exercises.set(await firstValueFrom(this.api.GetExercises()));
            this._listLoaded.set(true);
            return true;
        } catch {
            this._error.set('No se han podido cargar tus ejercicios');
            return false;
        } finally {
            this._loading.set(false);
        }
    }

    async LoadExercise(exerciseId: string): Promise<boolean> {
        this._error.set(null);
        this._notFound.set(false);
        this._detail.set(null);
        this._loading.set(true);
        try {
            this._detail.set(await firstValueFrom(this.api.GetExerciseProgress(exerciseId)));
            return true;
        } catch (error) {
            if (error instanceof HttpErrorResponse && error.status === 404) this._notFound.set(true);
            else this._error.set('No se ha podido cargar el progreso del ejercicio');
            return false;
        } finally {
            this._loading.set(false);
        }
    }
}
