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
export interface Session{
    id : string,
    name : string
}
export interface Set{
    exerciseId : string,
    repetitionRangeMin : number,
    repetitionRangeMax : number,
    expectedRIR : number,
    advanceTrainingTechniques : string,
    superSetGroupId : string | null,
    aimMuscleGroups : string | null
}

@Service()
export class TrainingApiService {
    private http = inject(HttpClient);
    private baseUrl = `${environment.apiUrl}/training`

    CreateMacrocycle(startAt:Date,endAt:Date | null,trainingStage:string,periodization:string,comments:string | null) : Observable<void>{
        return this.http.post<void>(`${this.baseUrl}/macrocycle`,
            {
                startAt,
                endAt,
                trainingStage,
                periodization,
                comments
            })
    }
    CreateMesocycle
    (
        macrocycleId:string,
        aimMuscleGroups :string [],
        motivationLevel:number,
        injuries:string | null,
        comments: string | null,
        startAt:Date,
        endAt:Date
    ) : Observable<void>{
        return this.http.post<void>(`${this.baseUrl}/mesocycle`,{
            macrocycleId,
            aimMuscleGroups,
            motivationLevel,
            injuries,
            comments,
            startAt,
            endAt
        })
    }
    CreateMicrocycle
    (
        mesocycleId : string,
        durationInDays: number,
        trainingDistribution: string

    ) : Observable<void>{
        return this.http.post<void>(`${this.baseUrl}/microcycle`,{
            mesocycleId,
            durationInDays,
            trainingDistribution
        });
    }
    CreateTrainingTemplate
    (
        name : string,
        volumeLandmark: string,
        trainingDistribution: string
    ) : Observable<{id: string}>{
        return this.http.post<{id: string}>(`${this.baseUrl}/training-template`,
            {
                name,
                volumeLandmark,
                trainingDistribution
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
        expectedRIR:number,
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
            expectedRIR,
            aimMuscleGroups,
            superSetGroupId
        })
    }
    GetAllExercises() : Observable<Exercise[]>{
        return this.http.get<Exercise[]>(`${this.baseUrl}/exercises`);
    }
    GetSessionsOfATrainingTemplate (TrainingTemplateId : string) : Observable<Session[]>{
        return this.http.get<Session[]>(`${this.baseUrl}/training-template/${TrainingTemplateId}/sessions`)
    }
    GetSetsOfASessionTemplate (TrainingTemplateId : string, SessionTemplateId : string) : Observable<Set[]>{
        return this.http.get<Set[]>(`${this.baseUrl}/training-template/${TrainingTemplateId}/${SessionTemplateId}/sets`);
    }

}
