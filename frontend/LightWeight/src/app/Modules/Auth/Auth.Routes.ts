import { Routes } from "@angular/router";
import { LoginPage } from "./UI/login-page/login-page";
import { VerifyOtpPage } from "./UI/verify-otp-page/verify-otp-page";

// auth.routes.ts
export const authRoutes: Routes = [
  { path: 'login', component: LoginPage },
  { path: 'verify', component: VerifyOtpPage },
];