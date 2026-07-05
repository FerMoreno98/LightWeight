import { inject, Injectable, signal } from "@angular/core";
import { AuthApiService } from "../data/auth-api.service";
import { firstValueFrom } from "rxjs";
import { TokenStorageService } from "../../../Core/Auth/token-storage.service";

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
    } catch {
      this._error.set('No se ha podido enviar el código');
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
    } catch {
      this._error.set('Código incorrecto');
      return false;
    } finally {
      this._isLoading.set(false);
    }
  }

  async logout(): Promise<void> {
    await firstValueFrom(this.api.logout());
    this.tokenStorage.clear();
  }
}