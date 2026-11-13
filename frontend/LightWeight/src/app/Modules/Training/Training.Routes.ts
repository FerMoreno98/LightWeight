import { Routes } from "@angular/router";
import { TrainingFlow } from "./UI/shared/training-flow";
import { OnboardingFlow } from "./UI/Onboarding/onboarding-flow";
import { OnboardingStore } from "./UI/Onboarding/onboarding.store";
import { resumeOnboardingGuard } from "./UI/Onboarding/onboarding.guards";

export const TrainingRoutes: Routes = [
    {
        // Onboarding wizard: the usual planning forms inside a layout with the minimap. OnboardingFlow makes
        // them move through the wizard pages. Only reachable by URL for now (no link points here yet)
        path: 'onboarding',
        providers: [{ provide: TrainingFlow, useClass: OnboardingFlow }, OnboardingStore],
        children: [
            // On its own: to the step where the user left it
            { path: '', pathMatch: 'full', canActivate: [resumeOnboardingGuard], children: [] },
            {
                path: '',
                loadComponent: () => import('./UI/Onboarding/onboarding-layout/onboarding-layout').then(m => m.OnboardingLayout),
                children: [
                    { path: 'program', loadComponent: () => import('./UI/Programs/create-program/create-program').then(m => m.CreateProgram) },
                    { path: ':programId/template', loadComponent: () => import('./UI/Templates/create-training-template/create-training-template').then(m => m.CreateTrainingTemplate) },
                    { path: ':programId/sessions/:id', loadComponent: () => import('./UI/Templates/create-session-template/create-session-template').then(m => m.CreateSessionTemplate) },
                    { path: ':programId/sets/:templateid/:sessionid', loadComponent: () => import('./UI/Templates/session-sets/session-sets').then(m => m.SessionSets) },
                    { path: ':programId/exercise/:templateid/:sessionid', loadComponent: () => import('./UI/Exercises/exercise-settings/exercise-settings').then(m => m.ExerciseSettings) },
                    { path: ':programId/levels', loadComponent: () => import('./UI/Onboarding/onboarding-levels/onboarding-levels').then(m => m.OnboardingLevels) },
                    { path: ':programId/launch', loadComponent: () => import('./UI/Onboarding/onboarding-launch/onboarding-launch').then(m => m.OnboardingLaunch) },
                ],
            },
        ],
    },
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
        path: 'session/:templateid/:sessionid',
        loadComponent: () => import('./UI/sessions/session-plan/session-plan').then(m => m.SessionPlan)
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
