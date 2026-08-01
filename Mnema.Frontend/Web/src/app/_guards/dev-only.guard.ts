import {CanActivateFn, Router} from '@angular/router';
import {inject} from "@angular/core";
import {environment} from "@env/environment";

export const devOnlyGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  return environment.production ? router.createUrlTree(['/']) : true;
};
