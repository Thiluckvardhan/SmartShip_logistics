import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, EMPTY, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  UserProfile,
  LoginDto,
  VerifyLoginOtpDto,
  ResendLoginOtpDto,
  RegisterDto,
  TokenDto,
  GoogleLoginDto,
  GoogleConfigResponse,
  ForgotPasswordDto,
  ResetPasswordDto,
  UpdateUserDto,
  AuthResponse
} from '../../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = environment.apiUrl;

  currentUserSubject = new BehaviorSubject<UserProfile | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    try {
      const stored = localStorage.getItem('user_profile');
      if (stored) {
        this.currentUserSubject.next(JSON.parse(stored));
      }
    } catch {
      // ignore malformed stored data
    }
  }

  register(dto: RegisterDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/auth/register`, dto);
  }

  login(dto: LoginDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/auth/login`, dto).pipe(
      tap(response => {
        if (response.requiresOtp) {
          return;
        }

        this.storeTokens(response);
        this.hydrateCurrentUser();
      })
    );
  }

  verifyLoginOtp(dto: VerifyLoginOtpDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/auth/verify-login-otp`, dto).pipe(
      tap(response => {
        this.storeTokens(response);
        this.hydrateCurrentUser();
      })
    );
  }

  resendLoginOtp(dto: ResendLoginOtpDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/auth/resend-login-otp`, dto);
  }

  googleLogin(dto: GoogleLoginDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/auth/google`, dto).pipe(
      tap(response => {
        this.storeTokens(response);
        this.hydrateCurrentUser();
      })
    );
  }

  getGoogleConfig(): Observable<GoogleConfigResponse> {
    return this.http.get<GoogleConfigResponse>(`${this.apiUrl}/api/auth/google-config`);
  }

  refreshToken(token: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/auth/refresh-token`, { token });
  }

  logout(): Observable<any> {
    const refreshToken = this.getRefreshToken();
    if (refreshToken) {
      return this.http.post(`${this.apiUrl}/api/auth/logout`, { token: refreshToken }).pipe(
        tap(() => this.clearStorage())
      );
    }
    this.clearStorage();
    return EMPTY;
  }

  forgotPassword(dto: ForgotPasswordDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/auth/forgot-password`, dto);
  }

  resetPassword(dto: ResetPasswordDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/auth/reset-password`, dto);
  }

  getMe(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.apiUrl}/api/users/me`);
  }

  updateMe(dto: UpdateUserDto): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.apiUrl}/api/users/me`, dto);
  }

  deleteMe(): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/users/me`);
  }

  isLoggedIn(): boolean {
    const token = localStorage.getItem('access_token');
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      // exp is in seconds; Date.now() is in ms
      if (payload.exp && payload.exp * 1000 < Date.now()) {
        this.clearStorage();
        return false;
      }
      return true;
    } catch {
      return false;
    }
  }

  getRole(): string | null {
    const token = this.getAccessToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return (
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
        payload['role'] ??
        null
      );
    } catch {
      return null;
    }
  }

  getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  storeTokens(response: AuthResponse): void {
    const access = response.accessToken ?? (response as any).AccessToken;
    const refresh = response.refreshToken ?? (response as any).RefreshToken;
    if (access) localStorage.setItem('access_token', access);
    if (refresh) localStorage.setItem('refresh_token', refresh);
  }

  clearStorage(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user_profile');
    this.currentUserSubject.next(null);
  }

  private hydrateCurrentUser(): void {
    this.getMe().subscribe({
      next: (user) => {
        localStorage.setItem('user_profile', JSON.stringify(user));
        this.currentUserSubject.next(user);
      },
      error: () => {
        this.clearStorage();
      }
    });
  }
}
