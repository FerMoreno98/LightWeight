import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../Environments/Environment';

export type Sex = 'Male' | 'Female';

/** The profile as the forms use it: the date of birth as YYYY-MM-DD, the value of an input type="date" */
export interface ProfileData {
    name: string;
    sex: Sex;
    dateOfBirth: string;
}

interface ProfileResponse {
    name: string;
    sex: Sex;
    /** ISO date, midnight in UTC */
    dateOfBirth: string;
}

@Service()
export class UserProfileApiService {
    private http = inject(HttpClient);
    private baseUrl = `${environment.apiUrl}/user-profile`;

    /** 404 while the user has not completed the onboarding */
    Get(): Observable<ProfileResponse> {
        return this.http.get<ProfileResponse>(`${this.baseUrl}/`);
    }

    /** Onboarding: creates the profile. 409 if it already exists */
    Complete(data: ProfileData): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/complete`, {
            fullName: data.name,
            birthDate: data.dateOfBirth,
            sex: data.sex,
        });
    }

    Update(data: ProfileData): Observable<void> {
        return this.http.put<void>(`${this.baseUrl}/`, {
            fullName: data.name,
            dateOfBirth: data.dateOfBirth,
            sex: data.sex,
        });
    }
}
