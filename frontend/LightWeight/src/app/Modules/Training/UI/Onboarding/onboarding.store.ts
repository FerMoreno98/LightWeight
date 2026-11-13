import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { LaunchTrainingBlockRequest, LaunchTrainingBlockResult, TrainingApiService, TrainingSetupStatus } from '../../data/training-api.service';

/**
 * State of the onboarding wizard. Separate from TrainingStore on purpose: refreshing the status after every page
 * must not toggle the isLoading the planning forms use for their submit buttons.
 * Provided on the wizard route, so it lives while the wizard is open
 */
@Injectable()
export class OnboardingStore {
  private api = inject(TrainingApiService);

  private _status = signal<TrainingSetupStatus | null>(null);
  private _error = signal<string | null>(null);
  private _isLaunching = signal(false);

  status = this._status.asReadonly();
  error = this._error.asReadonly();
  isLaunching = this._isLaunching.asReadonly();

  /** Reloads the status of the given program (the most recent one without it). Returns null if it fails */
  async refresh(programId?: string | null): Promise<TrainingSetupStatus | null> {
    try {
      const status = await firstValueFrom(this.api.GetTrainingSetupStatus(programId ?? undefined));
      this._status.set(status);
      this._error.set(null);
      return status;
    } catch {
      this._error.set('No se ha podido cargar el progreso del asistente');
      return null;
    }
  }

  async launch(request: LaunchTrainingBlockRequest): Promise<LaunchTrainingBlockResult | null> {
    this._isLaunching.set(true);
    this._error.set(null);
    try {
      return await firstValueFrom(this.api.LaunchTrainingBlock(request));
    } catch {
      this._error.set('No se ha podido lanzar el bloque');
      return null;
    } finally {
      this._isLaunching.set(false);
    }
  }
}
