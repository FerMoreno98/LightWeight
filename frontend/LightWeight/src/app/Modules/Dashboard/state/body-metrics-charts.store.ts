import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DashboardApiService, EvolutionPoint, NutritionEvolution, WeightEvolution } from '../data/dashboard-api.service';

const NO_NUTRITION: NutritionEvolution = { averageCalories: null, recentCalories: null, weeks: [] };

@Injectable({ providedIn: 'root' })
export class BodyMetricsChartsStore {
    private api = inject(DashboardApiService);

    private _weight = signal<WeightEvolution>({ points: [], weeklyAverages: [] });
    private _perimeters = signal<EvolutionPoint[]>([]);
    private _skinFolds = signal<EvolutionPoint[]>([]);
    private _nutrition = signal<NutritionEvolution>(NO_NUTRITION);
    private _loading = signal(false);
    private _loaded = signal(false);
    private _rebuilding = signal(false);
    private _error = signal<string | null>(null);

    weight = this._weight.asReadonly();
    perimeters = this._perimeters.asReadonly();
    skinFolds = this._skinFolds.asReadonly();
    /** Weeks of the FatSecret diary, drawn under the weight */
    nutrition = this._nutrition.asReadonly();
    loading = this._loading.asReadonly();
    /** Whether the data has been loaded at least once (to tell "loading" from "empty") */
    loaded = this._loaded.asReadonly();
    rebuilding = this._rebuilding.asReadonly();
    error = this._error.asReadonly();

    /** Loads the whole history of the three kinds at once */
    async Load(): Promise<boolean> {
        this._error.set(null);
        this._loading.set(true);
        try {
            const [weight, perimeters, skinFolds, nutrition] = await Promise.all([
                firstValueFrom(this.api.GetWeightEvolution()),
                firstValueFrom(this.api.GetPerimetersEvolution()),
                firstValueFrom(this.api.GetSkinFoldsEvolution()),
                // Not critical: without it the weight is drawn alone
                firstValueFrom(this.api.GetNutritionEvolution()).catch(() => NO_NUTRITION),
            ]);
            this._weight.set(weight);
            this._perimeters.set(perimeters);
            this._skinFolds.set(skinFolds);
            this._nutrition.set(nutrition);
            this._loaded.set(true);
            return true;
        } catch {
            this._error.set('No se ha podido cargar la evolución');
            return false;
        } finally {
            this._loading.set(false);
        }
    }

    /** Rebuilds the projections from the measurements and reloads the charts */
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
