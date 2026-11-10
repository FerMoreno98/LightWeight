import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthStore } from '../../../Auth/state/auth.store';
import { ProfileData } from '../../data/user-profile-api.service';
import { UserProfileStore } from '../../state/user-profile.store';
import { ProfileForm } from '../profile-form/profile-form';

/** Edit the profile and log out. The guard has already loaded it, unless it could not be checked */
@Component({
    selector: 'app-profile-page',
    standalone: true,
    imports: [ProfileForm],
    templateUrl: './profile-page.html',
})
export class ProfilePage implements OnInit {
    store = inject(UserProfileStore);
    private auth = inject(AuthStore);
    private router = inject(Router);

    private _loggingOut = signal(false);
    loggingOut = this._loggingOut.asReadonly();

    private _saved = signal(false);
    saved = this._saved.asReadonly();

    async ngOnInit() {
        this.store.clearError();
        await this.store.ensureLoaded();
    }

    async onSave(data: ProfileData) {
        this._saved.set(false);
        this._saved.set(await this.store.Update(data));
    }

    async logout() {
        this._loggingOut.set(true);
        await this.auth.logout();
        // The next user of this browser must not see this profile
        this.store.reset();
        await this.router.navigate(['/auth/login']);
    }
}
