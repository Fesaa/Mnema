import {
  ApplicationConfig, ErrorHandler,
  importProvidersFrom,
  inject,
  isDevMode,
  provideAppInitializer,
  provideZoneChangeDetection
} from '@angular/core';
import {provideRouter} from '@angular/router';

import {routes} from './app.routes';
import {provideHttpClient, withInterceptors} from "@angular/common/http";
import {BrowserAnimationsModule} from "@angular/platform-browser/animations";
import {APP_BASE_HREF, CommonModule, PlatformLocation} from "@angular/common";
import {ContentTitlePipe} from "./_pipes/content-title.pipe";
import {provideAnimationsAsync} from '@angular/platform-browser/animations/async';
import {ProviderNamePipe} from "./_pipes/provider-name.pipe";
import {SubscriptionExternalUrlPipe} from "./_pipes/subscription-external-url.pipe";
import {provideTransloco} from "@jsverse/transloco";
import {TranslocoLoaderImpl} from "./_services/transloco-loader";
import {AccountService} from './_services/account.service';
import {NavService} from "./_services/nav.service";
import {catchError, filter, firstValueFrom, Observable, of, switchMap, tap, timeout} from "rxjs";
import {provideToastr} from "ngx-toastr";
import {PageService} from "./_services/page.service";
import {RolePipe} from "./_pipes/role.pipe";
import {errorHandlerInterceptor} from "./_interceptors/error-handler.interceptor";

function getBaseHref(platformLocation: PlatformLocation): string {
  return platformLocation.getBaseHrefFromDOM();
}

function bootstrapUser() {
  const accountService = inject(AccountService);
  const navService = inject(NavService);
  const pageService = inject(PageService);

  return firstValueFrom(accountService.getMe().pipe(
    catchError(() => of(null)),
    switchMap(() => {
      const user = accountService.currentUser();
      if (!user) return of(null);

      return pageService.refreshPages().pipe(tap(() => {
        navService.setNavVisibility(true);
      }));
    })
  )).then(() => void 0);
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
    provideRouter(routes),

    provideHttpClient(withInterceptors([errorHandlerInterceptor])),

    provideToastr(),
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
    provideAppInitializer(() => bootstrapUser()),
  ]
};
