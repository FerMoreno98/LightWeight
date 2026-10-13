import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { environment } from '../../../../Environments/Environment';
import { Observable } from 'rxjs';

export interface Exercise {
    id: string;
    name: string;
    isBilateral: boolean;
    aimMuscleGroups: number[];
}
/** A session of a template, with the size of its plan */
export interface Session{
    id : string,
    name : string,
    setsCount : number,
    exercisesCount : number
}
export type MuscleGroup =
    | 'Shoulder' | 'Back' | 'Chest' | 'Biceps' | 'Triceps'
    | 'Glutes' | 'Quads' | 'Hamstring' | 'Calves';
export interface SeriesPerGroupPerSession{
    sessionId : string,
    sessionName : string,
    series : Partial<Record<MuscleGroup, number>>
}
export type VolumeLandmark = 'MV' | 'MEV' | 'MAV' | 'MRV';
export type Periodization = 'Linear' | 'Ondulating' | 'block' | 'MikeIsraetel';
export interface Program{
    id : string,
    name : string,
    periodization : Periodization,
    aimMuscleGroups : MuscleGroup[],
    trainingTemplatesCount : number
}
export type TrainingDistribution =
    | 'PushPullLegs' | 'UpperLower' | 'Weider' | 'Phat' | 'FullBody' | 'Other';
export interface TrainingTemplate{
    id : string,
    name : string,
    programId : string,
    programName : string,
    order : number,
    durationInDays : number,
    volumeLandmark : VolumeLandmark,
    trainingDistribution : TrainingDistribution,
    totalVolume : Partial<Record<MuscleGroup, number>>
}
export type AdvancedTechnique = 'DropSet' | 'Cluster' | 'MyoRep';
export interface Set{
    id : string,
    exerciseId : string,
    /** Empty when the exercise no longer exists */
    exerciseName : string,
    repetitionRangeMin : number,
    repetitionRangeMax : number,
    expectedRPE : number,
    /** Advanced technique of the set, null when it is a normal set */
    technique : AdvancedTechnique | null,
    superSetGroupId : string | null,
    aimMuscleGroups : MuscleGroup[]
}

export type TrainingStage = 'Bulk' | 'Cut' | 'Maintenance';
export interface MacrocycleSummary{
    id : string,
    stage : TrainingStage,
    comments : string | null,
    startedAt : string,
    finishedAt : string | null,
    mesocyclesCount : number
}
export interface MacrocycleMesocycle{
    id : string,
    programId : string,
    programName : string,
    startedAt : string,
    finishedAt : string | null,
    motivationLevel : number,
    injuries : string | null,
    comments : string | null,
    microcyclesCount : number
}
export interface MacrocycleDetail{
    id : string,
    stage : TrainingStage,
    comments : string | null,
    startedAt : string,
    finishedAt : string | null,
    mesocycles : MacrocycleMesocycle[]
}
export interface MesocycleMicrocycle{
    id : string,
    weekNumber : number,
    trainingTemplateId : string,
    /** null when the template has been deleted */
    templateName : string | null,
    durationInDays : number | null,
    startedAt : string,
    /** null while the week is in progress */
    finishedAt : string | null
}
export interface MesocycleTemplate{
    id : string,
    name : string,
    order : number,
    durationInDays : number,
    volumeLandmark : VolumeLandmark,
    trainingDistribution : TrainingDistribution
}
export interface MesocycleDetail{
    id : string,
    macrocycleId : string,
    programId : string,
    programName : string,
    startedAt : string,
    finishedAt : string | null,
    motivationLevel : number,
    injuries : string | null,
    comments : string | null,
    microcycles : MesocycleMicrocycle[],
    availableTemplates : MesocycleTemplate[]
}

export type CurrentMicrocycleStatus = 'NoActiveMacrocycle' | 'NoActiveMesocycle' | 'PendingPlanning' | 'InProgress';
/** The week the user is training now, or which step of the hierarchy is missing */
export interface CurrentMicrocycle{
    status : CurrentMicrocycleStatus,
    macrocycleId : string | null,
    mesocycleId : string | null,
    programId : string | null,
    programName : string | null,
    /** Only with PendingPlanning */
    nextWeekNumber : number | null,
    /** Only with InProgress */
    microcycle : {
        id : string,
        weekNumber : number,
        startedAt : string,
        trainingTemplateId : string,
        /** null when the template has been deleted */
        templateName : string | null,
        durationInDays : number | null
    } | null
}

@Service()
export class TrainingApiService {
    private http = inject(HttpClient);
    private baseUrl = `${environment.apiUrl}/training`

    CreateMacrocycle(trainingStage:string,comments:string | null) : Observable<{id: string}>{
        return this.http.post<{id: string}>(`${this.baseUrl}/macrocycle`,
            {
                trainingStage,
                comments
            })
    }
    GetUserMacrocycles() : Observable<MacrocycleSummary[]>{
        return this.http.get<MacrocycleSummary[]>(`${this.baseUrl}/macrocycles`);
    }
    GetMacrocycleDetail(macrocycleId : string) : Observable<MacrocycleDetail>{
        return this.http.get<MacrocycleDetail>(`${this.baseUrl}/macrocycle/${macrocycleId}`);
    }
    FinishMacrocycle(macrocycleId : string) : Observable<void>{
        return this.http.post<void>(`${this.baseUrl}/macrocycle/${macrocycleId}/finish`, {});
    }
    CreateProgram
    (
        name : string,
        periodization : string,
        aimMuscleGroups : string []
    ) : Observable<{id: string}>{
        return this.http.post<{id: string}>(`${this.baseUrl}/program`,
            {
                name,
                periodization,
                aimMuscleGroups
            });
    }
    GetUserPrograms () : Observable<Program[]>{
        return this.http.get<Program[]>(`${this.baseUrl}/programs`);
    }
    CreateMesocycle
    (
        macrocycleId:string,
        programId:string,
        motivationLevel:number,
        injuries:string | null,
        comments: string | null
    ) : Observable<{id: string}>{
        return this.http.post<{id: string}>(`${this.baseUrl}/mesocycle`,{
            macrocycleId,
            programId,
            motivationLevel,
            injuries,
            comments
        })
    }
    GetMesocycleDetail(mesocycleId : string) : Observable<MesocycleDetail>{
        return this.http.get<MesocycleDetail>(`${this.baseUrl}/mesocycle/${mesocycleId}`);
    }
    FinishMesocycle(mesocycleId : string) : Observable<void>{
        return this.http.post<void>(`${this.baseUrl}/mesocycle/${mesocycleId}/finish`, {});
    }
    CreateMicrocycle
    (
        mesocycleId : string,
        trainingTemplateId : string
    ) : Observable<{id: string}>{
        return this.http.post<{id: string}>(`${this.baseUrl}/microcycle`,{
            mesocycleId,
            trainingTemplateId
        });
    }
    GetCurrentMicrocycle() : Observable<CurrentMicrocycle>{
        return this.http.get<CurrentMicrocycle>(`${this.baseUrl}/microcycle/current`);
    }
    FinishMicrocycle(microcycleId : string) : Observable<void>{
        return this.http.post<void>(`${this.baseUrl}/microcycle/${microcycleId}/finish`, {});
    }
    CreateTrainingTemplate
    (
        programId : string,
        name : string,
        volumeLandmark: string,
        trainingDistribution: string,
        durationInDays : number
    ) : Observable<{id: string}>{
        return this.http.post<{id: string}>(`${this.baseUrl}/training-template`,
            {
                programId,
                name,
                volumeLandmark,
                trainingDistribution,
                durationInDays
            });
    }
    CreateTemplateSession
    (
        trainingTemplateId : string | null,
        name :string
    ) : Observable<{id: string}>{
        return this.http.post<{id: string}>(`${this.baseUrl}/template-session`,
            {
                name,
                trainingTemplateId
            }
        )
    }
    CreateTemplateSet
    (
        exerciseId:string | null,
        templateSessionId:string | null,
        min:number,
        max:number,
        isDropset:boolean,
        isCluster:boolean,
        isMyoRep:boolean,
        aimMuscleGroups : string [],
        expectedRPE:number,
        series:number,
        superSetGroupId :string | null
    ) : Observable<void>{
        return this.http.post<void>(`${this.baseUrl}/template-set`,{
            exerciseId,
            templateSessionId,
            min,
            max,
            isDropset,
            isCluster,
            isMyoRep,
            expectedRPE,
            series,
            aimMuscleGroups,
            superSetGroupId
        })
    }
    GetAllExercises() : Observable<Exercise[]>{
        return this.http.get<Exercise[]>(`${this.baseUrl}/exercises`);
    }
    GetSessionsOfATrainingTemplate (TrainingTemplateId : string) : Observable<Session[]>{
        return this.http.get<Session[]>(`${this.baseUrl}/training-template/${TrainingTemplateId}/sessions`);
    }
    GetSetsOfASessionTemplate (TrainingTemplateId : string, SessionTemplateId : string) : Observable<Set[]>{
        return this.http.get<Set[]>(`${this.baseUrl}/training-template/${TrainingTemplateId}/${SessionTemplateId}/sets`);
    }
    GetSeriesPerMuscleGroupPerSession (TrainingTemplateId : string) : Observable<SeriesPerGroupPerSession[]>{
        return this.http.get<SeriesPerGroupPerSession[]>(`${this.baseUrl}/training-session/${TrainingTemplateId}/seriespermusclegrouppersession`);
    }
    GetUserTrainingTemplates () : Observable<TrainingTemplate[]>{
        return this.http.get<TrainingTemplate[]>(`${this.baseUrl}/training-template/trainingTemplates`);
    }
    DeleteSetTemplate(TemplateSetId : string) : Observable<void>{
        return this.http.delete<void>(`${this.baseUrl}/training-set/${TemplateSetId}`);
    }
    DeleteTemplateSession(TemplateSessionId : string) : Observable<void>{
        return this.http.delete<void>(`${this.baseUrl}/training-session/${TemplateSessionId}`);
    }
    DeleteTrainingTemplate(TrainingTemplateId : string) : Observable<void>{
        return this.http.delete<void>(`${this.baseUrl}/training-template/${TrainingTemplateId}`);
    }
    DuplicateTrainingTemplate(TrainingTemplateId : string) : Observable<{id: string}>{
        return this.http.post<{id: string}>(`${this.baseUrl}/training-template/${TrainingTemplateId}/duplicate`, {});
    }
    RenameTrainingTemplate(TrainingTemplateId : string, name : string) : Observable<void>{
        return this.http.patch<void>(`${this.baseUrl}/training-template/${TrainingTemplateId}/name`, { name });
    }
    UpdateTemplateSet
    (
        templateSessionId:string | null,
        setId : string | null,
        min:number,
        max:number,
        isDropset:boolean,
        isCluster:boolean,
        isMyoRep:boolean,
        aimMuscleGroups : string [],
        expectedRPE:number,
        superSetGroupId :string | null
    ) : Observable<void>{
        return this.http.put<void>(`${this.baseUrl}/template-set`,{
            templateSessionId,
            setId,
            min,
            max,
            isDropset,
            isCluster,
            isMyoRep,
            expectedRPE,
            aimMuscleGroups,
            superSetGroupId
        
        })
    }

}
