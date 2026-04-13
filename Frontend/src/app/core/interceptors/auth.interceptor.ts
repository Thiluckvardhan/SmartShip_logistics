import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError, BehaviorSubject, filter, take } from 'rxjs';
import { AuthService } from '../services/auth.service';

let isRefreshing = false;
let refreshTokenSubject = new BehaviorSubject<any>(null);

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const accessToken = authService.getAccessToken();

  // Don't intercept token refresh requests to avoid loops
  if (req.url.includes('/api/auth/refresh-token')) {
    return next(req);
  }

  const authReq = req.clone({
    setHeaders: {
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      'X-Correlation-ID': crypto.randomUUID()
    }
  });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        if (!isRefreshing) {
          isRefreshing = true;
          refreshTokenSubject.next(null);
          
          const refreshToken = authService.getRefreshToken();
          if (refreshToken) {
            return authService.refreshToken(refreshToken).pipe(
              switchMap(response => {
                isRefreshing = false;
                authService.storeTokens(response);
                const newAccess = response.accessToken ?? (response as any).AccessToken;
                refreshTokenSubject.next(newAccess);

                const retryReq = req.clone({
                  setHeaders: {
                    Authorization: `Bearer ${newAccess}`,
                    'X-Correlation-ID': crypto.randomUUID()
                  }
                });
                return next(retryReq);
              }),
              catchError(refreshError => {
                isRefreshing = false;
                authService.clearStorage();
                router.navigate(['/login']);
                return throwError(() => refreshError);
              })
            );
          } else {
            isRefreshing = false;
            authService.clearStorage();
            router.navigate(['/login']);
            return throwError(() => error);
          }
        } else {
          // Queue requests while token is refreshing
          return refreshTokenSubject.pipe(
            filter(token => token != null),
            take(1),
            switchMap(token => {
              const retryReq = req.clone({
                setHeaders: {
                  Authorization: `Bearer ${token}`,
                  'X-Correlation-ID': crypto.randomUUID()
                }
              });
              return next(retryReq);
            })
          );
        }
      }

      return throwError(() => error);
    })
  );
};
