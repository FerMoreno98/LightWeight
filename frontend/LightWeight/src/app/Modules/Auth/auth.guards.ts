import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenStorageService } from '../../Core/Auth/token-storage.service';
import { RETURN_URL_PARAM, safeReturnUrl } from '../../Core/Navigation/return-url';

/**
 * The login pages only without session. The session is restored at startup (refresh cookie),
 * so a user who is already logged in goes straight to the page they wanted (returnUrl) or the home,
 * instead of having to ask for an OTP again
 */
export const guestGuard: CanActivateFn = route => {
    const router = inject(Router);
    const tokens = inject(TokenStorageService);

    if (!tokens.getAccessToken()) return true;
    const returnUrl = safeReturnUrl(route.queryParamMap.get(RETURN_URL_PARAM));
    return router.parseUrl(returnUrl ?? '/home');
};
