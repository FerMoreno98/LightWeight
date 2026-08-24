import { inject, Injectable, signal } from "@angular/core";
import { TrainingApiService } from "../data/training-api.service";
import { firstValueFrom } from "rxjs";

@Injectable({ providedIn: 'root' })
export class TrainingStore{
    private api= inject(TrainingApiService);

    private _isLoading = signal(false);
    private _error = signal<string | null>(null);

    isLoading = this._isLoading.asReadonly();
    error = this._error.asReadonly();

    async CreateMacrocycle
    (
        startAt: Date,
        endAt :Date | null,
        stage:string,
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
                    stage,
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
}