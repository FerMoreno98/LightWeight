import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ProfileData, UserProfileApiService } from '../data/user-profile-api.service';

/** unknown: not asked yet; missing: the onboarding is pending; error: could not be checked */
export type ProfileStatus = 'unknown' | 'missing' | 'complete' | 'error';

@Injectable({ providedIn: 'root' })
export class UserProfileStore {
    private api = inject(UserProfileApiService);

    private _profile = signal<ProfileData | null>(null);
    private _status = signal<ProfileStatus>('unknown');
    private _isSaving = signal(false);
    private _error = signal<string | null>(null);

    profile = this._profile.asReadonly();
    status = this._status.asReadonly();
    isSaving = this._isSaving.asReadonly();
    error = this._error.asReadonly();

    /** The request in flight, so the guards of one navigation share it */
    private loading: Promise<ProfileStatus> | null = null;

    /** Asks for the profile once per session; afterwards answers from memory */
    ensureLoaded(): Promise<ProfileStatus> {
        const status = this._status();
        if (status === 'missing' || status === 'complete') return Promise.resolve(status);
        this.loading ??= this.load().finally(() => (this.loading = null));
        return this.loading;
    }

    /** Onboarding. If the profile already existed (409) it is loaded instead, so the user goes on */
    async Complete(data: ProfileData): Promise<boolean> {
        return this.save(
            () => firstValueFrom(this.api.Complete(data)),
            data,
            'No se ha podido guardar el perfil. Revisa los datos e inténtalo de nuevo',
        );
    }

    async Update(data: ProfileData): Promise<boolean> {
        return this.save(
            () => firstValueFrom(this.api.Update(data)),
            data,
            'No se han podido guardar los cambios. Revisa los datos e inténtalo de nuevo',
        );
    }

    /** On login: the profile in memory may belong to the previous user */
    reset() {
        this._profile.set(null);
        this._status.set('unknown');
        this._error.set(null);
    }

    clearError() {
        this._error.set(null);
    }

    private async load(): Promise<ProfileStatus> {
        try {
            const profile = await firstValueFrom(this.api.Get());
            this._profile.set({
                name: profile.name,
                sex: profile.sex,
                // Stored as midnight UTC: the date part is the day the user chose
                dateOfBirth: profile.dateOfBirth.slice(0, 10),
            });
            this._status.set('complete');
        } catch (error) {
            this._status.set(error instanceof HttpErrorResponse && error.status === 404 ? 'missing' : 'error');
        }
        return this._status();
    }

    private async save(request: () => Promise<void>, data: ProfileData, errorMessage: string): Promise<boolean> {
        this._isSaving.set(true);
        this._error.set(null);
        try {
            await request();
            this._profile.set(data);
            this._status.set('complete');
            return true;
        } catch (error) {
            if (error instanceof HttpErrorResponse && error.status === 409) {
                // Completed before (another tab, a double click): take what is stored
                this._status.set('unknown');
                return (await this.ensureLoaded()) === 'complete';
            }
            this._error.set(errorMessage);
            return false;
        } finally {
            this._isSaving.set(false);
        }
    }
}
