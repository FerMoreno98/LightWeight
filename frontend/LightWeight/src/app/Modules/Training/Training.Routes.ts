import { Routes } from "@angular/router";

export const TrainingRoutes: Routes = [
    {
        path: 'macrocycles',
        loadComponent: () => import('./UI/macrocycles/macrocycle-list/macrocycle-list').then(m => m.MacrocycleList)
    },
    {
        path: 'macrocycle',
        loadComponent: () => import('./UI/macrocycles/macrocycle-create/macrocycle-create').then(m => m.MacrocicloCreatePage)

    },
    {
        path: 'macrocycle/:id',
        loadComponent: () => import('./UI/macrocycles/macrocycle-detail/macrocycle-detail').then(m => m.MacrocycleDetail)
    },
    {
        path: 'mesocycle/:id',
        loadComponent: () => import('./UI/mesocycles/mesocycle-detail/mesocycle-detail').then(m => m.MesocycleDetail)
    },
    {
        path:'createprogram',
        loadComponent: () => import('./UI/Programs/create-program/create-program').then(p => p.CreateProgram)
    },
    {
        path:'trainingtemplate',
        loadComponent: () => import('./UI/Templates/create-training-template/create-training-template').then(t=>t.CreateTrainingTemplate)
    },
    {
        path:'createtrainingtemplates',
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
    },
    {
        path:'trainingtemplates',
        loadComponent: ()=> import('./UI/Templates/training-templates/training-templates').then(t => t.TrainingTemplates)
    },
    
]
