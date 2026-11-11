import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthStore } from '../../state/auth.store';
import { ActivatedRoute, Router } from '@angular/router';
import { RETURN_URL_PARAM, returnUrlParams } from '../../../../Core/Navigation/return-url';

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
  private route = inject(ActivatedRoute);

  email = '';
  isLoading = this.authStore.isLoading;
  error = this.authStore.error;

  async onSubmit() {
    const success = await this.authStore.requestOtp(this.email);
    if (success) {
      // The page the user wanted (e.g. a shared program) goes on to the verification
      const returnUrl = this.route.snapshot.queryParamMap.get(RETURN_URL_PARAM);
      this.router.navigate(['/auth/verify'], { queryParams: { email: this.email, ...returnUrlParams(returnUrl) } });
    }
  }

}
