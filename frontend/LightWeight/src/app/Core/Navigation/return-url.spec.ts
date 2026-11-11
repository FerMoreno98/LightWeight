import { returnUrlParams, safeReturnUrl } from './return-url';

describe('safeReturnUrl', () => {
    it('accepts paths of the app, with query and fragment', () => {
        expect(safeReturnUrl('/share/Xk3J9qLm2Pz8')).toBe('/share/Xk3J9qLm2Pz8');
        expect(safeReturnUrl('/training/trainingtemplates?tab=1#top')).toBe('/training/trainingtemplates?tab=1#top');
    });

    it('rejects empty values', () => {
        expect(safeReturnUrl(null)).toBeNull();
        expect(safeReturnUrl(undefined)).toBeNull();
        expect(safeReturnUrl('')).toBeNull();
    });

    it('rejects other hosts (open redirect)', () => {
        expect(safeReturnUrl('https://evil.com')).toBeNull();
        expect(safeReturnUrl('//evil.com')).toBeNull();
        expect(safeReturnUrl('/\\evil.com')).toBeNull();
        expect(safeReturnUrl('javascript:alert(1)')).toBeNull();
    });

    it('rejects the pages of the login flow (loops)', () => {
        expect(safeReturnUrl('/auth/login')).toBeNull();
        expect(safeReturnUrl('/auth/verify?email=a@b.c')).toBeNull();
        expect(safeReturnUrl('/onboarding')).toBeNull();
        expect(safeReturnUrl('/onboarding?returnUrl=/home')).toBeNull();
    });

    it('does not confuse pages that only start like the flow ones', () => {
        expect(safeReturnUrl('/authors')).toBe('/authors');
    });
});

describe('returnUrlParams', () => {
    it('carries a safe url', () => {
        expect(returnUrlParams('/share/abc')).toEqual({ returnUrl: '/share/abc' });
    });

    it('carries nothing for unsafe urls or the home', () => {
        expect(returnUrlParams('//evil.com')).toEqual({});
        expect(returnUrlParams('/home')).toEqual({});
        expect(returnUrlParams(null)).toEqual({});
    });
});
