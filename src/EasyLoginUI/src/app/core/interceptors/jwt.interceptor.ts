import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError, Subject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

let isRefreshing = false;
const refreshDone$ = new Subject<boolean>();

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getAccessToken();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) return throwError(() => error);

      // Never retry refresh or login endpoints — would cause infinite loops
      if (req.url.includes('/auth/refresh') || req.url.includes('/auth/login')) {
        return throwError(() => error);
      }

      if (isRefreshing) {
        // Queue behind the in-flight refresh
        return refreshDone$.pipe(
          filter(success => success),
          take(1),
          switchMap(() => {
            const newToken = auth.getAccessToken();
            return next(req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } }));
          }),
        );
      }

      isRefreshing = true;
      return auth.refreshToken().pipe(
        switchMap(() => {
          isRefreshing = false;
          refreshDone$.next(true);
          const newToken = auth.getAccessToken();
          return next(req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } }));
        }),
        catchError(err => {
          isRefreshing = false;
          refreshDone$.next(false);
          auth.logout();
          return throwError(() => err);
        }),
      );
    }),
  );
};
