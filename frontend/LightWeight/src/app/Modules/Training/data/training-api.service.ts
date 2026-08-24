import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { environment } from '../../../../Environments/Environment';
import { Observable } from 'rxjs';

@Service()
export class TrainingApiService {
    private http = inject(HttpClient);
    private baseUrl = `${environment.apiUrl}/training`

    CreateMacrocycle(startAt:Date,endAt:Date | null,stage:string,periodization:string,comments:string | null) : Observable<void>{
        return this.http.post<void>(`${this.baseUrl}/macrocycle`,
            {
                startAt,
                endAt,
                stage,
                periodization,
                comments
            })
    }

}
