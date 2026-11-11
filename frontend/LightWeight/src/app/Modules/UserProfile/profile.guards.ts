import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenStorageService } from '../../Core/Auth/token-storage.service';
import { RETURN_URL_PARAM, returnUrlParams, safeReturnUrl } from '../../Core/Navigation/return-url';
import { UserProfileStore } from './state/user-profile.store';

/**
 * For every page of the app: without session goes to login, and without profile to the onboarding.
 * Both remember the page the user wanted (returnUrl), so a shared link opens after logging in.
 * If the profile cannot be checked (network, server) it lets the user in: the onboarding is not worth blocking the app
 */
export const profileCompletedGuard: CanActivateFn = async (_route, state) => {
    // Everything injected before the first await: afterwards there is no injection context
    const router = inject(Router);
    const tokens = inject(TokenStorageService);
    const store = inject(UserProfileStore);

    if (!tokens.getAccessToken())
        return router.createUrlTree(['/auth/login'], { queryParams: returnUrlParams(state.url) });
    const status = await store.ensureLoaded();
    return status === 'missing'
        ? router.createUrlTree(['/onboarding'], { queryParams: returnUrlParams(state.url) })
        : true;
};

/** The onboarding only for users with session and without profile. It keeps the returnUrl it received */
export const onboardingGuard: CanActivateFn = async route => {
    const router = inject(Router);
    const tokens = inject(TokenStorageService);
    const store = inject(UserProfileStore);
    const returnUrl = route.queryParamMap.get(RETURN_URL_PARAM);

    if (!tokens.getAccessToken())
        return router.createUrlTree(['/auth/login'], { queryParams: returnUrlParams(returnUrl) });
    const status = await store.ensureLoaded();
    return status === 'complete' ? router.parseUrl(safeReturnUrl(returnUrl) ?? '/home') : true;
};
