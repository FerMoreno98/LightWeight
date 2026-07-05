import { Routes } from '@angular/router';

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
    }
];
