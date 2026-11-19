import { Component, inject, OnInit, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ConfirmDialog } from '../../../../Shared/confirm-dialog/confirm-dialog';
import { FatSecretApiService, FatSecretConnection } from '../../data/fatsecret-api.service';

/**
 * Link or unlink the FatSecret account. Linking leaves the app: the browser goes to FatSecret and comes back to
 * /integrations/fatsecret/callback. Hidden when the server has no FatSecret keys or the status cannot be loaded
 */
@Component({
    selector: 'app-fatsecret-card',
    standalone: true,
    imports: [ConfirmDialog],
    templateUrl: './fatsecret-card.html',
})
export class FatSecretCard implements OnInit {
    private api = inject(FatSecretApiService);

    connection = signal<FatSecretConnection | null>(null);
    busy = signal(false);
    error = signal<string | null>(null);
    confirmingUnlink = signal(false);

    async ngOnInit() {
        try {
            this.connection.set(await firstValueFrom(this.api.Get()));
        } catch {
            this.connection.set(null);
        }
    }

    async link() {
        this.busy.set(true);
        this.error.set(null);
        try {
            const { authorizeUrl } = await firstValueFrom(this.api.StartLinking());
            window.location.href = authorizeUrl;
        } catch {
            this.error.set('No se ha podido conectar con FatSecret. Inténtalo de nuevo en un rato.');
            this.busy.set(false);
        }
    }

    async unlink() {
        this.busy.set(true);
        this.error.set(null);
        try {
            await firstValueFrom(this.api.Unlink());
            this.connection.update(c => (c ? { ...c, status: 'NotLinked', linkedAt: null, historyImported: false } : c));
        } catch {
            this.error.set('No se ha podido desconectar la cuenta. Inténtalo de nuevo.');
        } finally {
            this.busy.set(false);
            this.confirmingUnlink.set(false);
        }
    }

    formatDate(iso: string): string {
        return new Date(iso).toLocaleDateString('es-ES', { day: 'numeric', month: 'long', year: 'numeric' });
    }
}
