import {
  ApplicationConfig,
  importProvidersFrom,
  inject,
  isDevMode,
  provideAppInitializer,
  provideZoneChangeDetection
} from '@angular/core';
import {provideRouter, withComponentInputBinding} from '@angular/router';

import {routes} from './app.routes';
import {provideHttpClient, withInterceptors, withXhr} from "@angular/common/http";
import {BrowserAnimationsModule} from "@angular/platform-browser/animations";
import {APP_BASE_HREF, CommonModule, PlatformLocation} from "@angular/common";
import {ContentTitlePipe} from "./_pipes/content-title.pipe";
import {provideAnimationsAsync} from '@angular/platform-browser/animations/async';
import {ProviderNamePipe} from "./_pipes/provider-name.pipe";
import {SubscriptionExternalUrlPipe} from "./_pipes/subscription-external-url.pipe";
import {provideTransloco} from "@jsverse/transloco";
import {TranslocoLoaderImpl} from "./_services/transloco-loader";
import {NavService} from "./_services/nav.service";
import {filter, firstValueFrom, switchMap, tap} from "rxjs";
import {PageService} from "./_services/page.service";
import {RolePipe} from "./_pipes/role.pipe";
import {errorHandlerInterceptor} from "./_interceptors/error-handler.interceptor";
import {SettingsService} from "@mnema/_services/settings.service";
import {provideRetoast} from "ngx-retoast";

function getBaseHref(platformLocation: PlatformLocation): string {
  return platformLocation.getBaseHrefFromDOM();
}

function initilizer() {
  const navService = inject(NavService);
  const pageService = inject(PageService);
  const settingService = inject(SettingsService);

  return firstValueFrom(settingService.checkServerSetup().pipe(
    filter(b => b),
    switchMap(() => settingService.checkIsAuthenticated()),
    filter(b => b),
    switchMap(() => settingService.getConfig()),
    switchMap(() => pageService.refreshPages()),
    tap(() => navService.setNavVisibility(true))
  ), { defaultValue: null }).then(() => void 0);
}

export const appConfig: ApplicationConfig = {
  providers: [
    CommonModule,
    ContentTitlePipe,
    ProviderNamePipe,
    RolePipe,
    SubscriptionExternalUrlPipe,

    importProvidersFrom(BrowserAnimationsModule), provideAnimationsAsync(),
    provideZoneChangeDetection({eventCoalescing: true}),
    provideRouter(routes, withComponentInputBinding()),

    provideHttpClient(withXhr(), withInterceptors([errorHandlerInterceptor])),

    provideRetoast({
      positionClass: 'toast-bottom-right',
      preventDuplicates: true,
      duration: 5000
    }),
    provideTransloco({
      config: {
        availableLangs: ['en'],
        defaultLang: 'en',
        missingHandler: {
          useFallbackTranslation: true,
          allowEmpty: true,
        },
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoLoaderImpl,
    }),
    {
      provide: APP_BASE_HREF,
      useFactory: getBaseHref,
      deps: [PlatformLocation]
    },
    provideAppInitializer(() => initilizer()),
  ]
};
