import { HttpClient, HttpParams } from '@angular/common/http';
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
    } | null,
    /** Sessions started in the active week (empty unless InProgress) */
    trainingSessions : { id : string, templateSessionId : string | null, finishedAt : string | null }[]
}

/** A set actually performed in a session */
export interface PerformedSet{
    id : string,
    exerciseId : string,
    /** Empty when the exercise no longer exists */
    exerciseName : string,
    /** Planned set it covers, null for an extra set */
    templateSetId : string | null,
    repetitions : number,
    weight : number,
    rpe : number,
    isBodyWeight : boolean,
    /** Superset it was performed in (planned or improvised), null when it was performed alone */
    superSetGroupId : string | null
}
export interface WorkoutSession{
    id : string,
    startAt : string,
    /** null while the session is in progress */
    finishedAt : string | null,
    /** "hh:mm:ss(.fffffff)", zero while in progress */
    duration : string,
    motivationLevel : number,
    sleepLevel : number,
    domsLevel : number,
    comments : string | null,
    sets : PerformedSet[]
}
/** What has been done this week for a session of a template */
export interface SessionWorkout{
    /** True when the session belongs to the week in progress and has not been started */
    canStart : boolean,
    session : WorkoutSession | null
}
/** Last time an exercise was performed in the active macrocycle */
export interface PreviousExercise{
    exerciseId : string,
    performedAt : string,
    sets : { repetitions : number, weight : number, rpe : number, isBodyWeight : boolean }[]
}
export interface SetValues{
    exerciseId : string,
    repetitions : number,
    weight : number,
    rpe : number,
    isBodyWeight : boolean
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
    GetSessionWorkout(templateSessionId : string) : Observable<SessionWorkout>{
        return this.http.get<SessionWorkout>(`${this.baseUrl}/training-session/workout/${templateSessionId}`);
    }
    GetPreviousPerformance(exerciseIds : string[], excludeSessionId : string | null) : Observable<PreviousExercise[]>{
        let params = new HttpParams();
        for (const id of exerciseIds) params = params.append('exerciseIds', id);
        if (excludeSessionId) params = params.set('excludeSessionId', excludeSessionId);
        return this.http.get<PreviousExercise[]>(`${this.baseUrl}/training-session/previous`, { params });
    }
    StartTrainingSession
    (
        templateSessionId : string,
        motivationLevel : number,
        sleepLevel : number,
        domsLevel : number,
        comments : string | null
    ) : Observable<{id: string}>{
        return this.http.post<{id: string}>(`${this.baseUrl}/training-session`, {
            templateSessionId,
            motivationLevel,
            sleepLevel,
            domsLevel,
            comments
        });
    }
    RegisterSet(trainingSessionId : string, templateSetId : string | null, superSetGroupId : string | null, values : SetValues) : Observable<{id: string}>{
        return this.http.post<{id: string}>(`${this.baseUrl}/training-session/${trainingSessionId}/sets`, { ...values, templateSetId, superSetGroupId });
    }
    /** Joins registered sets in a superset, or separates them with null */
    AssignSuperSet(trainingSessionId : string, setIds : string[], superSetGroupId : string | null) : Observable<void>{
        return this.http.put<void>(`${this.baseUrl}/training-session/${trainingSessionId}/supersets`, { setIds, superSetGroupId });
    }
    UpdateSet(trainingSessionId : string, setId : string, values : SetValues) : Observable<void>{
        return this.http.put<void>(`${this.baseUrl}/training-session/${trainingSessionId}/sets/${setId}`, values);
    }
    RemoveSet(trainingSessionId : string, setId : string) : Observable<void>{
        return this.http.delete<void>(`${this.baseUrl}/training-session/${trainingSessionId}/sets/${setId}`);
    }
    /** weekFinished is true when this was the last pending session of the week and the week has been closed */
    FinishTrainingSession(trainingSessionId : string) : Observable<{weekFinished: boolean, weekNumber: number}>{
        return this.http.post<{weekFinished: boolean, weekNumber: number}>(`${this.baseUrl}/training-session/${trainingSessionId}/finish`, {});
    }
    /** Deletes a session in progress with all its sets */
    DiscardTrainingSession(trainingSessionId : string) : Observable<void>{
        return this.http.post<void>(`${this.baseUrl}/training-session/${trainingSessionId}/discard`, {});
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
        series:number
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
            aimMuscleGroups
        })
    }
    /** Joins planned sets of a session in a superset (at least two exercises) */
    CreateTemplateSuperSet(templateSessionId : string, templateSetIds : string[]) : Observable<{id: string}>{
        return this.http.post<{id: string}>(`${this.baseUrl}/template-session/${templateSessionId}/supersets`, { templateSetIds });
    }
    RemoveTemplateSuperSet(templateSessionId : string, superSetGroupId : string) : Observable<void>{
        return this.http.delete<void>(`${this.baseUrl}/template-session/${templateSessionId}/supersets/${superSetGroupId}`);
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
        expectedRPE:number
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
            aimMuscleGroups
        
        })
    }

}
