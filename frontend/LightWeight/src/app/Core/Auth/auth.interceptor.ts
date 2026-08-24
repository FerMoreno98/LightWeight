import {
  HttpErrorResponse,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, firstValueFrom, from, switchMap, throwError } from 'rxjs';
import { TokenStorageService } from './token-storage.service';
import { AuthApiService } from '../../Modules/Auth/data/auth-api.service';

let isRefreshing = false;
let pendingRetries: Array<(token: string | null) => void> = [];

async function refreshAccessToken(): Promise<string | null> {
  // Si ya hay un refresh en curso, esperamos a que termine en vez de lanzar otro
  if (isRefreshing) {
    return new Promise((resolve) => pendingRetries.push(resolve));
  }

  isRefreshing = true;
  const api = inject(AuthApiService);
  const tokenStorage = inject(TokenStorageService);

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
  const tokenStorage = inject(TokenStorageService);

  // Las rutas de /auth no necesitan token y un 401 suyo no debe disparar refresh
  if (req.url.includes('/auth/')) {
    return next(req);
  }

  const token = tokenStorage.getAccessToken();
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      return from(refreshAccessToken()).pipe(
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
