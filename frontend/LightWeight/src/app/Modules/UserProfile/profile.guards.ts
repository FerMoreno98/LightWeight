import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenStorageService } from '../../Core/Auth/token-storage.service';
import { UserProfileStore } from './state/user-profile.store';

/**
 * For every page of the app: without session goes to login, and without profile to the onboarding.
 * If the profile cannot be checked (network, server) it lets the user in: the onboarding is not worth blocking the app
 */
export const profileCompletedGuard: CanActivateFn = async () => {
    // Everything injected before the first await: afterwards there is no injection context
    const router = inject(Router);
    const tokens = inject(TokenStorageService);
    const store = inject(UserProfileStore);

    if (!tokens.getAccessToken()) return router.parseUrl('/auth/login');
    const status = await store.ensureLoaded();
    return status === 'missing' ? router.parseUrl('/onboarding') : true;
};

/** The onboarding only for users with session and without profile */
export const onboardingGuard: CanActivateFn = async () => {
    const router = inject(Router);
    const tokens = inject(TokenStorageService);
    const store = inject(UserProfileStore);

    if (!tokens.getAccessToken()) return router.parseUrl('/auth/login');
    const status = await store.ensureLoaded();
    return status === 'complete' ? router.parseUrl('/home') : true;
};
