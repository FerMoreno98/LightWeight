import { inject, ApplicationConfig, provideBrowserGlobalErrorListeners, provideAppInitializer } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './Core/Auth/auth.interceptor';
import { AuthStore } from './Modules/Auth/state/auth.store';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    // Al recargar la pagina, recupera el access token con la cookie de refresh
    provideAppInitializer(() => inject(AuthStore).restoreSession())
  ]
};
