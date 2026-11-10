
import { Routes } from '@angular/router';
import { onboardingGuard, profileCompletedGuard } from './Modules/UserProfile/profile.guards';

export const routes: Routes =
[
    {
        path: '',
        redirectTo: '/auth/login',
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
        path: 'profile',
        canActivate: [profileCompletedGuard],
        loadChildren: () => import ('../app/Modules/UserProfile/UserProfile.Routes').then(m=>m.UserProfileRoutes)
    }
];
