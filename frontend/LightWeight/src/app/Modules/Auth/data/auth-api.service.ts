import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../Environments/Environment';

@Service()
export class AuthApiService {
    private http = inject(HttpClient);
    private baseUrl = `${environment.apiUrl}/auth`

    requestOtp(email : string) : Observable<void>
    {
        return this.http.post<void>(`${this.baseUrl}/otp/send`,{email});
    }
    verifyOtp(code: string, deviceIdentifier: string, deviceName: string, platform: string, email: string): Observable<string>
    {
        return this.http.post<string>(`${this.baseUrl}/otp/verify`, {
            email,
            code,
            deviceIdentifier,
            deviceName,
            platform,
        });
    }
    logout() : Observable<void>
    {
        return this.http.post<void>(`${this.baseUrl}/logout`,{});
    }
}
