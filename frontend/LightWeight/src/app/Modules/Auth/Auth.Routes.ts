import { Routes } from "@angular/router";
import { LoginPage } from "./UI/login-page/login-page";
import { VerifyOtpPage } from "./UI/verify-otp-page/verify-otp-page";
import { guestGuard } from "./auth.guards";

// auth.routes.ts
export const authRoutes: Routes = [
  { path: 'login', canActivate: [guestGuard], component: LoginPage },
  { path: 'verify', canActivate: [guestGuard], component: VerifyOtpPage },
];