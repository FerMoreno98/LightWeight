import { Routes } from "@angular/router";

export const TrainingRoutes: Routes = [
    {
        path: 'macrocycle',
        loadComponent: () => import('./UI/macrocycles/macrocycle-create/macrocycle-create').then(m => m.MacrocicloCreatePage)

    },
    {
        path:'trainingtemplate',
        loadComponent: () => import('./UI/Templates/create-training-template/create-training-template').then(t=>t.CreateTrainingTemplate)
    },
    {
        path:'sessiontemplate/:id',
        loadComponent: () => import('./UI/Templates/create-session-template/create-session-template').then(s => s.CreateSessionTemplate)
    },
    {
        path:'exercisesettings/:templateid/:sessionid',
        loadComponent: () => import('./UI/Exercises/exercise-settings/exercise-settings').then(e=>e.ExerciseSettings)
    },
    {
        path:'sessionsets/:templateid/:sessionid',
        loadComponent: () => import('./UI/Templates/session-sets/session-sets').then(s=>s.SessionSets)
    }
]
