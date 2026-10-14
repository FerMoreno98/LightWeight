import { inject, Injectable, signal } from "@angular/core";
import { Exercise, Session, TrainingApiService, Set, SeriesPerGroupPerSession, TrainingTemplate, Program, MacrocycleSummary, MacrocycleDetail, MesocycleDetail, CurrentMicrocycle, SessionWorkout, PreviousExercise, SetValues } from "../data/training-api.service";
import { firstValueFrom } from "rxjs";

@Injectable({ providedIn: 'root' })
export class TrainingStore{
    private api= inject(TrainingApiService);

    private _isLoading = signal(false);
    private _error = signal<string | null>(null);
    private _exercises = signal<Exercise[]>([]);
    private _sessions = signal<Session[]>([]);
    private _sets = signal<Set[]>([]);
    private _seriesPerGroupPerSession = signal<SeriesPerGroupPerSession[]>([]);
    private _trainingTemplates = signal<TrainingTemplate[]>([]);
    private _programs = signal<Program[]>([]);
    private _macrocycles = signal<MacrocycleSummary[]>([]);
    private _macrocycleDetail = signal<MacrocycleDetail | null>(null);
    private _mesocycleDetail = signal<MesocycleDetail | null>(null);
    private _currentMicrocycle = signal<CurrentMicrocycle | null>(null);
    private _sessionWorkout = signal<SessionWorkout | null>(null);
    /** Last performance of each exercise, by exercise id. Merged as new exercises are requested */
    private _previousPerformance = signal<Record<string, PreviousExercise>>({});

    isLoading = this._isLoading.asReadonly();
    error = this._error.asReadonly();
    exercises = this._exercises.asReadonly();
    sessions = this._sessions.asReadonly();
    sets = this._sets.asReadonly();
    seriesPerGroupPerSession = this._seriesPerGroupPerSession.asReadonly();
    trainingTemplates = this._trainingTemplates.asReadonly();
    programs = this._programs.asReadonly();
    macrocycles = this._macrocycles.asReadonly();
    macrocycleDetail = this._macrocycleDetail.asReadonly();
    mesocycleDetail = this._mesocycleDetail.asReadonly();
    currentMicrocycle = this._currentMicrocycle.asReadonly();
    sessionWorkout = this._sessionWorkout.asReadonly();
    previousPerformance = this._previousPerformance.asReadonly();

    async CreateMacrocycle
    (
        trainingStage:string,
        comments:string | null
    ) : Promise<string | null>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const result = await firstValueFrom(this.api.CreateMacrocycle(trainingStage, comments));
            return result.id;
        }catch{
            this._error.set('No se ha podido crear el macrociclo');
            return null;
        }finally{
            this._isLoading.set(false);
        }
    }

    async GetUserMacrocycles() : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            this._macrocycles.set(await firstValueFrom(this.api.GetUserMacrocycles()));
            return true;
        }catch{
            this._error.set('No se han podido cargar los macrociclos');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }

    /** Loads a macrocycle with its mesocycles. The previous detail is cleared so another macrocycle is never shown */
    async GetMacrocycleDetail(macrocycleId : string) : Promise<boolean>{
        if (this._macrocycleDetail()?.id !== macrocycleId) this._macrocycleDetail.set(null);
        this._isLoading.set(true);
        this._error.set(null);
        try{
            this._macrocycleDetail.set(await firstValueFrom(this.api.GetMacrocycleDetail(macrocycleId)));
            return true;
        }catch{
            this._error.set('No se ha podido cargar el macrociclo');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }

    /** Finishes the macrocycle (and its active mesocycle) and reloads its detail */
    async FinishMacrocycle(macrocycleId : string) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.FinishMacrocycle(macrocycleId));
        }catch{
            this._error.set('No se ha podido finalizar el macrociclo');
            return false;
        }
        return this.GetMacrocycleDetail(macrocycleId);
    }

    async CreateMesocycle
    (
        macrocycleId:string,
        programId:string,
        motivationLevel:number,
        injuries:string | null,
        comments: string | null
    ) : Promise<string | null>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const result = await firstValueFrom(this.api.CreateMesocycle
                (
                    macrocycleId,
                    programId,
                    motivationLevel,
                    injuries,
                    comments
                ));
            return result.id;
        }catch{
            this._error.set('No se ha podido crear el mesociclo');
            return null;
        }finally{
            this._isLoading.set(false);
        }
    }

    /** Loads a mesocycle with its weeks and the templates available. The previous detail is cleared first */
    async GetMesocycleDetail(mesocycleId : string) : Promise<boolean>{
        if (this._mesocycleDetail()?.id !== mesocycleId) this._mesocycleDetail.set(null);
        this._isLoading.set(true);
        this._error.set(null);
        try{
            this._mesocycleDetail.set(await firstValueFrom(this.api.GetMesocycleDetail(mesocycleId)));
            return true;
        }catch{
            this._error.set('No se ha podido cargar el mesociclo');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }

    /** Finishes the mesocycle and reloads its detail */
    async FinishMesocycle(mesocycleId : string) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.FinishMesocycle(mesocycleId));
        }catch{
            this._error.set('No se ha podido finalizar el mesociclo');
            return false;
        }
        return this.GetMesocycleDetail(mesocycleId);
    }

    /** Adds the next week to the mesocycle and reloads its detail */
    async CreateMicrocycle
    (
        mesocycleId : string,
        trainingTemplateId : string
    ) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.CreateMicrocycle(mesocycleId, trainingTemplateId));
        }catch{
            this._error.set('No se ha podido añadir la semana');
            return false;
        }
        return this.GetMesocycleDetail(mesocycleId);
    }

    /** Loads the week the user is training now (or which step is missing) */
    async GetCurrentMicrocycle() : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            this._currentMicrocycle.set(await firstValueFrom(this.api.GetCurrentMicrocycle()));
            return true;
        }catch{
            this._error.set('No se ha podido cargar la semana actual');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }

    /** Finishes a week. The caller reloads the view it comes from (home or mesocycle detail) */
    async FinishMicrocycle(microcycleId : string) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.FinishMicrocycle(microcycleId));
            return true;
        }catch{
            this._error.set('No se ha podido finalizar la semana');
            return false;
        }
    }

    /** Loads what has been done this week for a session of a template */
    async GetSessionWorkout(templateSessionId : string) : Promise<boolean>{
        this._error.set(null);
        try{
            this._sessionWorkout.set(await firstValueFrom(this.api.GetSessionWorkout(templateSessionId)));
            return true;
        }catch{
            this._error.set('No se ha podido cargar el entrenamiento');
            return false;
        }
    }

    /** Loads the last performance of the given exercises and merges it with the ones already loaded */
    async GetPreviousPerformance(exerciseIds : string[], excludeSessionId : string | null, reset = false) : Promise<boolean>{
        if (reset) this._previousPerformance.set({});
        const missing = exerciseIds.filter((id, i) => exerciseIds.indexOf(id) === i && !this._previousPerformance()[id]);
        if (missing.length === 0) return true;
        try{
            const previous = await firstValueFrom(this.api.GetPreviousPerformance(missing, excludeSessionId));
            this._previousPerformance.update(current => {
                const next = { ...current };
                for (const exercise of previous) next[exercise.exerciseId] = exercise;
                return next;
            });
            return true;
        }catch{
            this._error.set('No se han podido cargar los registros anteriores');
            return false;
        }
    }

    /** Starts the session and reloads the workout */
    async StartTrainingSession
    (
        templateSessionId : string,
        motivationLevel : number,
        sleepLevel : number,
        domsLevel : number,
        comments : string | null
    ) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.StartTrainingSession(templateSessionId, motivationLevel, sleepLevel, domsLevel, comments));
        }catch{
            this._error.set('No se ha podido iniciar el entrenamiento');
            return false;
        }
        return this.GetSessionWorkout(templateSessionId);
    }

    /** Registers a performed set (templateSetId null for an extra set) and reloads the workout */
    async RegisterSet(templateSessionId : string, trainingSessionId : string, templateSetId : string | null, superSetGroupId : string | null, values : SetValues) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.RegisterSet(trainingSessionId, templateSetId, superSetGroupId, values));
        }catch{
            this._error.set('No se ha podido registrar la serie');
            return false;
        }
        return this.GetSessionWorkout(templateSessionId);
    }

    /** Joins registered sets in a superset (or separates them with null) and reloads the workout */
    async AssignSuperSet(templateSessionId : string, trainingSessionId : string, setIds : string[], superSetGroupId : string | null) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.AssignSuperSet(trainingSessionId, setIds, superSetGroupId));
        }catch{
            this._error.set('No se ha podido guardar la superserie');
            return false;
        }
        return this.GetSessionWorkout(templateSessionId);
    }

    /** Joins planned sets in a superset and reloads the sets of the session */
    async CreateTemplateSuperSet(trainingTemplateId : string, templateSessionId : string, templateSetIds : string[]) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.CreateTemplateSuperSet(templateSessionId, templateSetIds));
        }catch{
            this._error.set('No se ha podido crear la superserie');
            return false;
        }
        return this.GetSetsFromASessionTemplate(trainingTemplateId, templateSessionId);
    }

    /** Separates a superset of a session and reloads its sets */
    async RemoveTemplateSuperSet(trainingTemplateId : string, templateSessionId : string, superSetGroupId : string) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.RemoveTemplateSuperSet(templateSessionId, superSetGroupId));
        }catch{
            this._error.set('No se ha podido separar la superserie');
            return false;
        }
        return this.GetSetsFromASessionTemplate(trainingTemplateId, templateSessionId);
    }

    /** Corrects a registered set and reloads the workout */
    async UpdateSet(templateSessionId : string, trainingSessionId : string, setId : string, values : SetValues) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.UpdateSet(trainingSessionId, setId, values));
        }catch{
            this._error.set('No se ha podido guardar la serie');
            return false;
        }
        return this.GetSessionWorkout(templateSessionId);
    }

    /** Removes a registered set and reloads the workout */
    async RemoveSet(templateSessionId : string, trainingSessionId : string, setId : string) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.RemoveSet(trainingSessionId, setId));
        }catch{
            this._error.set('No se ha podido borrar la serie');
            return false;
        }
        return this.GetSessionWorkout(templateSessionId);
    }

    /**
     * Finishes the session and reloads the workout.
     * Returns whether it completed (and closed) the week, or null if it could not be finished
     */
    async FinishTrainingSession(templateSessionId : string, trainingSessionId : string) : Promise<{weekFinished: boolean, weekNumber: number} | null>{
        this._error.set(null);
        try{
            const result = await firstValueFrom(this.api.FinishTrainingSession(trainingSessionId));
            await this.GetSessionWorkout(templateSessionId);
            return result;
        }catch{
            this._error.set('No se ha podido finalizar el entrenamiento');
            return null;
        }
    }

    /** Deletes the session in progress with its sets and reloads the workout, so it can be started again */
    async DiscardTrainingSession(templateSessionId : string, trainingSessionId : string) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.DiscardTrainingSession(trainingSessionId));
        }catch{
            this._error.set('No se ha podido descartar el entrenamiento');
            return false;
        }
        return this.GetSessionWorkout(templateSessionId);
    }

    async CreateProgram
    (
        name : string,
        periodization : string,
        aimMuscleGroups : string []
    ) : Promise<string | null>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const result = await firstValueFrom(this.api.CreateProgram
                (
                    name,
                    periodization,
                    aimMuscleGroups
                ));
            return result.id;
        }catch{
            this._error.set('No se ha podido crear el programa');
            return null;
        }finally{
            this._isLoading.set(false);
        }
    }

    async GetUserPrograms() : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const programs = await firstValueFrom(this.api.GetUserPrograms());
            this._programs.set(programs);
            return true;
        }catch{
            this._error.set('No se han podido cargar los programas');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }

    async CreateTrainingTemplate
    (
        programId : string,
        name : string,
        volumeLandmark: string,
        trainingDistribution: string,
        durationInDays : number
    ) : Promise<string | null>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const result = await firstValueFrom(this.api.CreateTrainingTemplate
                (
                    programId,
                    name,
                    volumeLandmark,
                    trainingDistribution,
                    durationInDays
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
        expectedRPE:number,
        series:number
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
                    expectedRPE,
                    series
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
    /** Loads the sessions of a template. The previous list is cleared so another template is never shown */
    async GetSessionsFromATrainingTemplate(TrainingTemplateId : string) : Promise<boolean>{
        this._sessions.set([]);
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
    async GetSeriesPerMuscleGroupPerSession(TrainingTemplateId : string) : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const series = await firstValueFrom(this.api.GetSeriesPerMuscleGroupPerSession(TrainingTemplateId));
            this._seriesPerGroupPerSession.set(series);
            return true;
        }catch{
            this._error.set('No se han podido cargar las sesiones');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }
    async GetUserTrainingTemplates() : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const templates = await firstValueFrom(this.api.GetUserTrainingTemplates());
            this._trainingTemplates.set(templates);
            return true;
        }catch{
            this._error.set('No se han podido cargar las plantillas de entrenamiento');
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }
    async DeleteSetTemplate(TemplateSetId : string) : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            await firstValueFrom(this.api.DeleteSetTemplate(TemplateSetId))
            this._sets.update(sets => sets.filter(s => s.id !== TemplateSetId));
            return true;
        }catch{
            this._error.set("No se ha podido borrar la serie");
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }
    async DeleteTemplateSession(TemplateSessionId : string) : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            await firstValueFrom(this.api.DeleteTemplateSession(TemplateSessionId))
            this._sessions.update(s => s.filter(s => s.id !== TemplateSessionId));
            this._seriesPerGroupPerSession.update(s => s.filter(s => s.sessionId !== TemplateSessionId));
            return true;
        }catch{
            this._error.set("No se ha podido borrar la sesion");
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }
    async DeleteTrainingTemplate(TrainingTemplateId : string) : Promise<boolean>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            await firstValueFrom(this.api.DeleteTrainingTemplate(TrainingTemplateId));
            const deleted = this._trainingTemplates().find(tt => tt.id === TrainingTemplateId);
            this._trainingTemplates.update(tt => tt.filter(tt => tt.id !== TrainingTemplateId));
            if (deleted){
                this._programs.update(ps => ps.map(p => p.id === deleted.programId
                    ? { ...p, trainingTemplatesCount: p.trainingTemplatesCount - 1 }
                    : p));
            }
            return true;
        }catch{
            this._error.set("No se ha podido eliminar la plantilla");
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }
    /** Duplicates a template with its sessions and sets, then reloads templates and programs
     *  (the copy's volume and the program's template count are computed by the backend) */
    async DuplicateTrainingTemplate(TrainingTemplateId : string) : Promise<string | null>{
        this._isLoading.set(true);
        this._error.set(null);
        try{
            const result = await firstValueFrom(this.api.DuplicateTrainingTemplate(TrainingTemplateId));
            const [templates, programs] = await Promise.all([
                firstValueFrom(this.api.GetUserTrainingTemplates()),
                firstValueFrom(this.api.GetUserPrograms()),
            ]);
            this._trainingTemplates.set(templates);
            this._programs.set(programs);
            return result.id;
        }catch{
            this._error.set("No se ha podido duplicar la plantilla");
            return null;
        }finally{
            this._isLoading.set(false);
        }
    }
    /** Renames a template and updates it in the local list (no reload needed) */
    async RenameTrainingTemplate(TrainingTemplateId : string, name : string) : Promise<boolean>{
        this._error.set(null);
        try{
            await firstValueFrom(this.api.RenameTrainingTemplate(TrainingTemplateId, name));
            this._trainingTemplates.update(tt => tt.map(t => t.id === TrainingTemplateId ? { ...t, name } : t));
            return true;
        }catch{
            this._error.set("No se ha podido renombrar la plantilla");
            return false;
        }
    }
    async UpdateTemplateSet
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
    ){
        this._isLoading.set(true);
        this._error.set(null);
        try{
            await firstValueFrom(this.api.UpdateTemplateSet
                (
                    templateSessionId,
                    setId,
                    min,
                    max,
                    isDropset,
                    isCluster,
                    isMyoRep,
                    aimMuscleGroups,
                    expectedRPE
                ))
            return true;
        }catch{
            this._error.set("No se ha podido actualizar el set");
            return false;
        }finally{
            this._isLoading.set(false);
        }
    }
}