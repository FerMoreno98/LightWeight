import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map } from 'rxjs';

interface NavSection {
  /** First segment of the URL that belongs to the section */
  key: string;
  label: string;
  link: string;
  /** SVG path(s) of a 24x24 stroke icon */
  icon: string;
}

/**
 * Main navigation: top bar on desktop, bottom tab bar on mobile (reachable with the thumb).
 * Hidden on the auth pages.
 */
@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
  private readonly router = inject(Router);

  protected readonly sections: NavSection[] = [
    { key: 'home', label: 'Inicio', link: '/home', icon: 'M3 11l9-7 9 7v9a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z' },
    { key: 'training', label: 'Entrenar', link: '/training/macrocycles', icon: 'M2 12h3M19 12h3M5 8v8M19 8v8M8 6v12M16 6v12M8 12h8' },
    { key: 'progress', label: 'Progreso', link: '/progress', icon: 'M4 19h16M6 15l4-4 3 3 5-6' },
    { key: 'bodymetrics', label: 'Medidas', link: '/bodymetrics', icon: 'M3 9a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2zM7 7v4M11 7v3M15 7v4' },
  ];

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  protected readonly currentSection = computed(() => this.url().split(/[/?#]/)[1] ?? '');
  protected readonly visible = computed(() => this.currentSection() !== 'auth');
}
