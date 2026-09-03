import { inject, Injectable, signal } from "@angular/core";
import { Exercise, Session, TrainingApiService, Set } from "../data/training-api.service";
import { firstValueFrom } from "rxjs";

@Injectable({ providedIn: 'root' })
export class TrainingStore{
    private api= inject(TrainingApiService);

    private _isLoading = signal(false);
    private _error = signal<string | null>(null);
    private _exercises = signal<Exercise[]>([]);
    private _sessions = signal<Session[]>([]);
    private _sets = signal<Set[]>([]);

    isLoading = this._isLoading.asReadonly();
    error = this._error.asReadonly();
    exercises = this._exercises.asReadonly();
    sessions = this._sessions.asReadonly();
    sets = this._sets.asReadonly();

    async CreateMacrocycle
    (
        startAt: Date,
        endAt :Date | null,
        trainingStage:string,
        periodization:string,
        comments:string | null
    ) : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null); 
        try{
            await firstValueFrom(this.api.CreateMacrocycle
                (
                    startAt,
                    endAt,
                    trainingStage,
                    periodization,
                    comments
                ));
            return true;
        }catch{
            this._error.set('No se ha podido crear el macrociclo');
            return false;
        }finally{

            this._isLoading.set(false);
        }

    }

    async CreateMesocycle
    (
        macrocycleId:string,
        aimMuscleGroups :string [],
        motivationLevel:number,
        injuries:string | null,
        comments: string | null,
        startAt:Date,
        endAt:Date
    ) : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            await firstValueFrom(this.api.CreateMesocycle
                (
                    macrocycleId,
                    aimMuscleGroups,
                    motivationLevel,
                    injuries,
                    comments,
                    startAt,
                    endAt
                ));
            return true;
        }catch{
            this._error.set('No se ha podido crear el mesociclo');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }

    async CreateMicrocycle
    (
        mesocycleId : string,
        durationInDays: number,
        trainingDistribution: string
    ) : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            await firstValueFrom(this.api.CreateMicrocycle
                (
                    mesocycleId,
                    durationInDays,
                    trainingDistribution
                ));
            return true;
        }catch{
            this._error.set('No se ha podido crear el microciclo');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }

    async CreateTrainingTemplate
    (
        name : string,
        volumeLandmark: string,
        trainingDistribution: string
    ) : Promise<string | null>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const result = await firstValueFrom(this.api.CreateTrainingTemplate
                (
                    name,
                    volumeLandmark,
                    trainingDistribution
                ));
            return result.id;
        }catch{
            this._error.set('No se ha podido crear la plantilla de entrenamiento');
            return null;
        }finally{
            this._isLoading.set(false);
        }
    }

    async CreateTemplateSession
    (
        trainingTemplateId : string | null,
        name :string
    ) : Promise<string | null>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const result = await firstValueFrom(this.api.CreateTemplateSession
                (
                    trainingTemplateId,
                    name
                ));
            return result.id;
        }catch{
            this._error.set('No se ha podido crear la sesión de la plantilla');
            return null;
        }finally{
            this._isLoading.set(false);
        }
    }

    async CreateTemplateSet
    (
        exerciseId:string | null,
        templateSessionId:string | null,
        min:number,
        max:number,
        isDropset:boolean,
        isCluster:boolean,
        isMyoRep:boolean,
        aimMuscleGroups : string [],
        expectedRIR:number,
        superSetGroupId :string | null
    ) : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            await firstValueFrom(this.api.CreateTemplateSet
                (
                    exerciseId,
                    templateSessionId,
                    min,
                    max,
                    isDropset,
                    isCluster,
                    isMyoRep,
                    aimMuscleGroups,
                    expectedRIR,
                    superSetGroupId
                ));
            return true;
        }catch{
            this._error.set('No se ha podido crear el set de la plantilla');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }

    async GetAllExercises() : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const exercises = await firstValueFrom(this.api.GetAllExercises());
            this._exercises.set(exercises);
            return true;
        }catch{
            this._error.set('No se han podido cargar los ejercicios');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }
    async GetSessionsFromATrainingTemplate(TrainingTemplateId : string) : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const sessions = await firstValueFrom(this.api.GetSessionsOfATrainingTemplate(TrainingTemplateId));
            this._sessions.set(sessions);
            return true;
        }catch{
            this._error.set('No se han podido cargar las sesiones');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }
    async GetSetsFromASessionTemplate(TrainingTemplateId : string, SessionTemplateId : string ) : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const sets = await firstValueFrom(this.api.GetSetsOfASessionTemplate(TrainingTemplateId,SessionTemplateId));
            this._sets.set(sets);
            return true;
        }catch{
            this._error.set('No se han podido cargar las sesiones');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }
}