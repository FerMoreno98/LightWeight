import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ProfileData } from '../../data/user-profile-api.service';
import { UserProfileStore } from '../../state/user-profile.store';
import { ProfileForm } from '../profile-form/profile-form';

/** Onboarding: shown after the first login, until the profile is completed (see onboardingGuard) */
@Component({
    selector: 'app-complete-profile-page',
    standalone: true,
    imports: [ProfileForm],
    templateUrl: './complete-profile-page.html',
})
export class CompleteProfilePage implements OnInit {
    store = inject(UserProfileStore);
    private router = inject(Router);

    ngOnInit() {
        this.store.clearError();
    }

    async onSave(data: ProfileData) {
        if (await this.store.Complete(data)) {
            this.router.navigate(['/home']);
        }
    }
}
