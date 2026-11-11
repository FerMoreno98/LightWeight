import { inject, Injectable, signal } from "@angular/core";
import { AuthApiService } from "../data/auth-api.service";
import { firstValueFrom } from "rxjs";
import { TokenStorageService } from "../../../Core/Auth/token-storage.service";
import { HttpErrorResponse } from "@angular/common/http";

// The API limits the code requests and checks per IP and answers 429 when they are exceeded
const TOO_MANY_ATTEMPTS = 'Demasiados intentos. Espera unos minutos y vuelve a probar';
const tooManyAttempts = (error: unknown) => error instanceof HttpErrorResponse && error.status === 429;

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private api = inject(AuthApiService);
  private tokenStorage = inject(TokenStorageService); // vive en core/auth


  private _isLoading = signal(false);
  private _error = signal<string | null>(null);

  isLoading = this._isLoading.asReadonly();
  error = this._error.asReadonly();
  

  async requestOtp(email: string): Promise<boolean> {
    this._isLoading.set(true);
    this._error.set(null);
    try {
      await firstValueFrom(this.api.requestOtp(email));
      return true;
    } catch (error) {
      this._error.set(tooManyAttempts(error) ? TOO_MANY_ATTEMPTS : 'No se ha podido enviar el código');
      return false;
    } finally {
      this._isLoading.set(false);
    }
  }

  async verifyOtp(code : string,DeviceIdentifier : string,DeviceName : string, Platform : string ,email : string ): Promise<boolean> {
    this._isLoading.set(true);
    this._error.set(null);
    try {
      const accessToken = await firstValueFrom(this.api.verifyOtp(code, DeviceIdentifier,DeviceName,Platform,email));
      this.tokenStorage.setAccessToken(accessToken);
      return true;
    } catch (error) {
      this._error.set(tooManyAttempts(error) ? TOO_MANY_ATTEMPTS : 'Código incorrecto');
      return false;
    } finally {
      this._isLoading.set(false);
    }
  }

  // Aunque falle la llamada (sin red, token caducado) la sesión se cierra en este dispositivo
  async logout(): Promise<void> {
    try {
      await firstValueFrom(this.api.logout());
    } catch {
      // El refresh token caducará solo
    } finally {
      this.tokenStorage.clear();
    }
  }

  // Se llama al arrancar la app: si hay cookie de refresh, recupera el access token
  // sin obligar al usuario a volver a hacer login
  async restoreSession(): Promise<void> {
    if (this.tokenStorage.getAccessToken()) {
      return;
    }
    try {
      const token = await firstValueFrom(this.api.refreshToken());
      this.tokenStorage.setAccessToken(token);
    } catch {
      // Sin sesion previa: se queda sin token y la app redirige a login
    }
  }
}