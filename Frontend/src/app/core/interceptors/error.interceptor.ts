import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notificationService = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // 401 is handled by the authInterceptor directly to intercept and refresh token.
      if (error.status !== 401) {
        if (error.status === 403) {
          notificationService.error('Access denied');
        } else if (error.status === 400 && error.error?.message) {
          notificationService.error(error.error.message);
        } else if (error.status === 404) {
          notificationService.error('Resource not found');
        } else if (error.status === 500) {
          notificationService.error('Server error, please try again');
        } else if (error.status === 0) {
          notificationService.error('Network error, please check your connection');
        } else {
          // Fallback error
          const msg = error.error?.message || error.message || 'An unexpected error occurred';
          notificationService.error(msg);
        }
      }

      return throwError(() => error);
    })
  );
};
