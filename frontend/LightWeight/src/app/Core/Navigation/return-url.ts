/**
 * Return to the page the user wanted after the login (and the onboarding), e.g. a shared program link.
 * The url travels as a query param: guard → /auth/login → /auth/verify → (/onboarding) → the page
 */
export const RETURN_URL_PARAM = 'returnUrl';

/** Pages that must never be the destination: they would send the user back into the login flow */
const FLOW_PREFIXES = ['/auth', '/onboarding'];

/**
 * The url if it is safe to navigate to it, null otherwise.
 * Only paths of this app: starting with one "/" (not "//" or "/\", which the browser reads as another host)
 * and outside the login flow, to avoid open redirects and loops
 */
export function safeReturnUrl(url: string | null | undefined): string | null {
    if (!url || !url.startsWith('/') || url.startsWith('//') || url.startsWith('/\\')) return null;
    const path = url.split(/[?#]/)[0];
    if (FLOW_PREFIXES.some(prefix => path === prefix || path.startsWith(prefix + '/'))) return null;
    return url;
}

/** Query params that carry the return url, or none if it is not safe (or it is just the home) */
export function returnUrlParams(url: string | null | undefined): Record<string, string> {
    const safe = safeReturnUrl(url);
    return safe && safe !== '/home' ? { [RETURN_URL_PARAM]: safe } : {};
}
