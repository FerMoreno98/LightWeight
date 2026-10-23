import { Routes } from '@angular/router';

/** Progreso: the history of each exercise, read from the dashboard */
export const DashboardRoutes: Routes = [
    {
        path: '',
        loadComponent: () => import('./UI/progress-list/progress-list').then(m => m.ProgressList),
    },
    {
        path: ':exerciseId',
        loadComponent: () => import('./UI/exercise-progress/exercise-progress').then(m => m.ExerciseProgressPage),
    },
];
