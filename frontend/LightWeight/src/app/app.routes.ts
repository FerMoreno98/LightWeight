
import { Routes } from '@angular/router';
import { onboardingGuard, profileCompletedGuard } from './Modules/UserProfile/profile.guards';

export const routes: Routes =
[
    {
        // The guard of /home sends to the login when there is no session
        path: '',
        redirectTo: '/home',
        pathMatch: 'full',
    },
    {
        path: 'auth',
        loadChildren: () => import('../app/Modules/Auth/Auth.Routes').then(m => m.authRoutes),
    },
    {
        // First login: the profile has to be completed before entering
        path: 'onboarding',
        canActivate: [onboardingGuard],
        loadComponent: () => import('../app/Modules/UserProfile/UI/complete-profile-page/complete-profile-page').then(m => m.CompleteProfilePage)
    },
    {
        path: 'training',
        canActivate: [profileCompletedGuard],
        loadChildren: () => import ('../app/Modules/Training/Training.Routes').then(m=>m.TrainingRoutes)
    },
    {
        path: 'bodymetrics',
        canActivate: [profileCompletedGuard],
        loadChildren: () => import ('../app/Modules/BodyMetrics/BodyMetrics.Routes').then(m=>m.BodyMetricsRoutes)
    },
    {
        path: 'progress',
        canActivate: [profileCompletedGuard],
        loadChildren: () => import ('../app/Modules/Dashboard/Dashboard.Routes').then(m=>m.DashboardRoutes)
    },
    {
        path: 'home',
        canActivate: [profileCompletedGuard],
        loadChildren: () => import ('../app/Modules/Home/Home.Routes').then(m=>m.homeRoutes)
    },
    {
        // Preview of a program shared by link, with the button to copy it. Needs a session (and a profile):
        // the guard sends to the login and comes back here afterwards (returnUrl)
        path: 'share/:token',
        canActivate: [profileCompletedGuard],
        loadComponent: () => import('../app/Modules/Training/UI/Share/shared-program/shared-program').then(m => m.SharedProgramPage)
    },
    {
        path: 'profile',
        canActivate: [profileCompletedGuard],
        loadChildren: () => import ('../app/Modules/UserProfile/UserProfile.Routes').then(m=>m.UserProfileRoutes)
    }
];
