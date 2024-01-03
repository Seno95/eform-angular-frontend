import {inject, Injectable} from '@angular/core';
import {ActivatedRouteSnapshot, CanActivateFn, Router, RouterStateSnapshot} from '@angular/router';
import {Observable, take} from 'rxjs';
import {map, switchMap, tap} from 'rxjs/operators';
import {Store} from '@ngrx/store';
import {selectAuthIsAuth} from 'src/app/state';

@Injectable()
export class AdminGuard {
  // private selectAuthIsAuth$ = this.authStore.select(selectAuthIsAuth);
  constructor(
    private router: Router,
    private store: Store
  ) {
    console.log('AdminGuard - constructor');
  }

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    console.log('AdminGuard - canActivate');
    // TODO: Fix this
    // return true;
    return this.store.select(selectAuthIsAuth).pipe(
      take(1), // Ensure the subscription is automatically unsubscribed after the first emission
      tap((isAuth) => {
        if (!isAuth) {
          console.debug(`Let's kick the user out admin.guard`);
          this.router.navigate(['/auth']).then();
        }
      }),
      switchMap(() => this.store.select(selectAuthIsAuth)),
      take(1) // Ensure the subscription is automatically unsubscribed after the first emission
    );
  }

  // checkGuards(guards: string[]): Observable<boolean> {
  //   return this.selectCurrentUserClaims$.pipe(map(x => {
  //     for (const guard of guards) {
  //       if (x[guard]) {
  //         return true;
  //       }
  //     }
  //     return false;
  //   }));
  // }
}

export const IsAdminGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> => {
  return inject(AdminGuard).canActivate(route, state);
}
