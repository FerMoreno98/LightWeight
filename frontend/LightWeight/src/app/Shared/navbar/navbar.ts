import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
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

  closeMenus() {
    this.menuOpen = false;
    this.desktopMenuOpen = false;
  }
}
