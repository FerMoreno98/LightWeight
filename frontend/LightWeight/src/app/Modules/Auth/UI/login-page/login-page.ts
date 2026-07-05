import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthStore } from '../../state/auth.store';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login-page',
  standalone:true,
  imports: [FormsModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.css',
})
export class LoginPage {
  private authStore = inject(AuthStore);
  private router = inject(Router);

  email = '';
  isLoading = this.authStore.isLoading;
  error = this.authStore.error;

  async onSubmit() {
    const success = await this.authStore.requestOtp(this.email);
    if (success) {
      this.router.navigate(['/auth/verify'], { queryParams: { email: this.email } });
    }
  }

}
