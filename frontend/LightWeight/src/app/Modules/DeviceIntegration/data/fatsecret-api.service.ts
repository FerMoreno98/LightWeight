import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../Environments/Environment';

/** Revoked: FatSecret rejected the token (the user removed the access or it expired): link again */
export type FatSecretStatus = 'NotLinked' | 'Linked' | 'Revoked';

export interface FatSecretConnection {
    /** False when the server has no FatSecret keys: the card is not shown */
    available: boolean;
    status: FatSecretStatus;
    /** ISO, UTC */
    linkedAt: string | null;
    /** False while the diary of a just linked account is being imported */
    historyImported: boolean;
}

@Service()
export class FatSecretApiService {
    private http = inject(HttpClient);
    private baseUrl = `${environment.apiUrl}/deviceintegration/fatsecret`;

    Get(): Observable<FatSecretConnection> {
        return this.http.get<FatSecretConnection>(`${this.baseUrl}/`);
    }

    /** The FatSecret page where the user authorizes LightWeight. It sends them back to /integrations/fatsecret/callback */
    StartLinking(): Observable<{ authorizeUrl: string }> {
        return this.http.post<{ authorizeUrl: string }>(`${this.baseUrl}/link`, null);
    }

    /** With the oauth_token and oauth_verifier of the callback query string */
    CompleteLinking(oauthToken: string, oauthVerifier: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/link/complete`, { oauthToken, oauthVerifier });
    }

    Unlink(): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/`);
    }
}
