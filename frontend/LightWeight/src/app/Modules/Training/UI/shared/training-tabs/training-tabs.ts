import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

/** Sub-navigation of the "Entrenar" section, shown on its top-level pages */
@Component({
  selector: 'app-training-tabs',
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="mb-6 inline-flex rounded-lg border border-line bg-surface p-1" aria-label="Secciones de entrenamiento">
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
  `,
})
export class TrainingTabs {
  protected readonly tabs = [
    { label: 'Macrociclos', link: '/training/macrocycles' },
    { label: 'Plantillas', link: '/training/trainingtemplates' },
  ];
}
