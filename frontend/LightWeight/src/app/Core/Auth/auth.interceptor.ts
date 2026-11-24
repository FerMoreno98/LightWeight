import {
  HttpErrorResponse,
  HttpInterceptorFn,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, firstValueFrom, from, switchMap, throwError } from 'rxjs';
import { TokenStorageService } from './token-storage.service';
import { AuthApiService } from '../../Modules/Auth/data/auth-api.service';

let isRefreshing = false;
let pendingRetries: Array<(token: string | null) => void> = [];

/**
 * Pide un access token nuevo con la cookie de refresco. Recibe los servicios en vez de inyectarlos:
 * se llama desde un catchError, cuando ya no hay contexto de inyección y inject() lanzaría NG0203
 */
async function refreshAccessToken(api: AuthApiService, tokenStorage: TokenStorageService): Promise<string | null> {
  // Si ya hay un refresh en curso, esperamos a que termine en vez de lanzar otro
  if (isRefreshing) {
    return new Promise((resolve) => pendingRetries.push(resolve));
  }

  isRefreshing = true;
  try {
    const newToken = await firstValueFrom(api.refreshToken());
    tokenStorage.setAccessToken(newToken);
    return newToken;
  } catch {
    // Cookie caducada o inexistente: sesión perdida
    tokenStorage.clear();
    return null;
  } finally {
    isRefreshing = false;
    pendingRetries.forEach((resolve) => resolve(tokenStorage.getAccessToken()));
    pendingRetries = [];
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Todos los inject() aquí arriba: solo funcionan mientras se ejecuta el interceptor, no en los callbacks del pipe
  const tokenStorage = inject(TokenStorageService);
  const api = inject(AuthApiService);

  const token = tokenStorage.getAccessToken();
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  // Las rutas de /auth no disparan refresh con un 401. Llevan el token igualmente: logout lo necesita
  if (req.url.includes('/auth/')) {
    return next(authReq);
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      return from(refreshAccessToken(api, tokenStorage)).pipe(
        switchMap((newToken) => {
          if (!newToken) {
            return throwError(() => error);
          }
          // Reintentamos la petición original con el token nuevo
          return next(
            req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } })
          );
        })
      );
    })
  );
};
