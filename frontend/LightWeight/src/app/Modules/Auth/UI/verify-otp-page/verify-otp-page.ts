import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthStore } from '../../state/auth.store';
import { ActivatedRoute, Router } from '@angular/router';
import { DeviceService } from '../../../../Core/device.service';
import { UserProfileStore } from '../../../UserProfile/state/user-profile.store';

@Component({
  selector: 'app-verify-otp-page',
  standalone:true,
  imports: [FormsModule],
  templateUrl: './verify-otp-page.html',
  styleUrl: './verify-otp-page.css',
})
export class VerifyOtpPage {
  private authStore = inject(AuthStore);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private device = inject(DeviceService);
  private profileStore = inject(UserProfileStore);

  code = '';
  email = '';
  isLoading = this.authStore.isLoading;
  error = this.authStore.error;

  constructor() {
    this.route.queryParams.subscribe(params => {
      this.email = params['email'] || '';
    });
  }

  async onSubmit() {
    const success = await this.authStore.verifyOtp(
      this.code,
      this.device.getDeviceIdentifier(),
      this.device.getDeviceName(),
      this.device.getPlatform(),
      this.email,
    );
    if (success) {
      // The profile in memory may be of a previous user. The guard of /home sends to the onboarding
      // if this user has not completed it yet (first login)
      this.profileStore.reset();
      this.router.navigate(['/home']);
    }
  }
}
