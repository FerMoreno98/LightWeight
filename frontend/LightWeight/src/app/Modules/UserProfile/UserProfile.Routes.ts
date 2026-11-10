import { Routes } from '@angular/router';

export const UserProfileRoutes: Routes = [
    {
        path: '',
        loadComponent: () => import('./UI/profile-page/profile-page').then(m => m.ProfilePage),
    },
];
