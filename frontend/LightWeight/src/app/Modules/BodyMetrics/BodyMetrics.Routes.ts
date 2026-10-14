import { Routes } from "@angular/router";

export const BodyMetricsRoutes: Routes = [
    {
        path: '',
        loadComponent: () => import('./UI/body-metrics-page/body-metrics-page').then(m => m.BodyMetricsPage)
    },
];
