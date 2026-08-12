import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SettingsService } from '@mnema/_services/settings.service';

export const authGuard: CanActivateFn = () => {
  const settingsService = inject(SettingsService);
  const router = inject(Router);

  return settingsService.isAuthenticated() ? true : router.createUrlTree(['/login']);
};
