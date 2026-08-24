import { Component } from '@angular/core';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [],
  templateUrl: './home-page.html',
  styleUrl: './home-page.css',
})
export class HomePage {
  menuOpen = false;
  trainingMenuOpen = false;
  desktopMenuOpen = false;

  openMenu() {
    this.menuOpen = true;
  }

  closeMenu() {
    this.menuOpen = false;
  }

  toggleTrainingMenu() {
    this.trainingMenuOpen = !this.trainingMenuOpen;
  }

  toggleDesktopMenu() {
    this.desktopMenuOpen = !this.desktopMenuOpen;
  }
}
