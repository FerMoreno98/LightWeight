import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { FatSecretApiService } from '../../data/fatsecret-api.service';

/**
 * FatSecret sends the user back here with ?oauth_token=…&oauth_verifier=…. The API checks that the token belongs
 * to the linking this user started, so the account is tied to whoever is logged in
 */
@Component({
    selector: 'app-fatsecret-callback-page',
    standalone: true,
    imports: [RouterLink],
    template: `
      <main class="min-h-screen px-4 py-10">
        <div class="mx-auto max-w-md">
          <h1 class="font-display mb-6 text-4xl leading-none font-bold text-fg uppercase">FatSecret</h1>
          @if (error(); as message) {
            <p class="alert-error" role="alert">{{ message }}</p>
            <a routerLink="/profile" class="btn btn-secondary mt-6 w-full">Volver al perfil</a>
          } @else {
            <p class="text-sm text-muted" role="status">Conectando tu cuenta...</p>
          }
        </div>
      </main>
    `,
})
export class FatSecretCallbackPage implements OnInit {
    private api = inject(FatSecretApiService);
    private route = inject(ActivatedRoute);
    private router = inject(Router);

    error = signal<string | null>(null);

    async ngOnInit() {
        const params = this.route.snapshot.queryParamMap;
        const token = params.get('oauth_token');
        const verifier = params.get('oauth_verifier');
        if (!token || !verifier) {
            this.error.set('No se ha autorizado el acceso en FatSecret.');
            return;
        }
        try {
            await firstValueFrom(this.api.CompleteLinking(token, verifier));
            await this.router.navigate(['/profile'], { replaceUrl: true });
        } catch (error) {
            // 400/409: expired or not the request in progress. The message of the API is in English
            this.error.set(
                error instanceof HttpErrorResponse && (error.status === 400 || error.status === 409)
                    ? 'La autorización ha caducado o no corresponde a esta sesión. Vuelve a conectar desde tu perfil.'
                    : 'No se ha podido conectar la cuenta. Inténtalo de nuevo desde tu perfil.',
            );
        }
    }
}
