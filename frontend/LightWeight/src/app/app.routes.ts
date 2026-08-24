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
    },
    {
        path: 'training',
        loadChildren: () => import ('../app/Modules/Training/Training.Routes').then(m=>m.TrainingRoutes)
    },
    {
        path: 'home',
        loadChildren: () => import ('../app/Modules/Home/Home.Routes').then(m=>m.homeRoutes)
    }
];
