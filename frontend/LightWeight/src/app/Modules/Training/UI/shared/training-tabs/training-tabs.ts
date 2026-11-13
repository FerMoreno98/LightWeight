import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

/** Sub-navigation of the "Entrenar" section, shown on its top-level pages */
@Component({
  selector: 'app-training-tabs',
  imports: [RouterLink, RouterLinkActive],
  template: `
    <div class="mb-6 flex flex-wrap items-center justify-between gap-3">
      <nav class="inline-flex rounded-lg border border-line bg-surface p-1" aria-label="Secciones de entrenamiento">
        @for (tab of tabs; track tab.link) {
          <a
            [routerLink]="tab.link"
            routerLinkActive="bg-raised text-fg"
            ariaCurrentWhenActive="page"
            class="rounded-md px-4 py-2 text-sm font-semibold text-muted transition-colors hover:text-fg"
          >
            {{ tab.label }}
          </a>
        }
      </nav>
      <!-- The animated map of how programs, templates and cycles fit together -->
      <a routerLink="/training/onboarding/intro" class="inline-flex min-h-11 items-center gap-2 text-sm font-medium text-muted transition hover:text-fg">
        <span class="grid h-6 w-6 place-items-center rounded-full border border-line-strong text-xs font-bold" aria-hidden="true">?</span>
        ¿Cómo funciona?
      </a>
    </div>
  `,
})
export class TrainingTabs {
  protected readonly tabs = [
    { label: 'Macrociclos', link: '/training/macrocycles' },
    { label: 'Plantillas', link: '/training/trainingtemplates' },
  ];
}
