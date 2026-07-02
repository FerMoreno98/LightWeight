import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';

@Service()
export class AuthApiService {
    private http = inject(HttpClient);
    private baseUrl = '/api/auth'

    requestOtp(email : string) : Observable<void>
    {
        return this.http.post<void>(`${this.baseUrl}/otp/request`,{email});
    }
    verifyOtp(email : string, code : string) : Observable<AuthResponseDto>
    {
        return this.http.post<AuthResponseDto>(`${this.baseUrl}/otp/verify`, { email, code });
    }
    logout() : Observable<void>
    {
        return this.http.post<void>(`${this.baseUrl}/logout`,{});
    }
}
