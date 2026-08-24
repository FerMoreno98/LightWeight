import { Routes } from "@angular/router";

export const TrainingRoutes: Routes = [
    {
        path: 'macrocycle',
        loadComponent: () => import('./UI/macrociclos/macrociclo-create/macrociclo-create').then(m => m.MacrocicloCreatePage)
    }
]
