import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {SettingsService} from '@mnema/_services/settings.service';

export const setupGuard: CanActivateFn = () => {
  const settingsService = inject(SettingsService);
  const router = inject(Router);

  return !settingsService.isServerSetup() ? router.createUrlTree(['/initial-setup']) : true;
};
