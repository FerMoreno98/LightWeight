import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { RETURN_URL_PARAM, safeReturnUrl } from '../../../../Core/Navigation/return-url';
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
    private route = inject(ActivatedRoute);

    ngOnInit() {
        this.store.clearError();
    }

    async onSave(data: ProfileData) {
        if (await this.store.Complete(data)) {
            // Back to the page the user wanted before the login (e.g. a shared program)
            const returnUrl = safeReturnUrl(this.route.snapshot.queryParamMap.get(RETURN_URL_PARAM));
            this.router.navigateByUrl(returnUrl ?? '/home');
        }
    }
}
