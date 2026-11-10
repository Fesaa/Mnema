import {Routes} from "@angular/router";
import {SettingsComponent} from "../settings/settings/settings.component";

export const routes: Routes = [
  {
    path: '',
    component: SettingsComponent,
    children: [
      {path: '', pathMatch: 'full', redirectTo: 'preferences'},
      {
        path: 'preferences',
        loadComponent: () => import('@mnema/settings/settings/_components/preference-settings/preference-settings.component')
          .then(m => m.PreferenceSettingsComponent)
      },
      {
        path: 'pages',
        loadComponent: () => import('@mnema/settings/settings/_components/pages-settings/pages-settings.component')
          .then(m => m.PagesSettingsComponent)
      },
      {
        path: 'providers',
        loadComponent: () => import('@mnema/settings/settings/_components/providers-settings/providers-settings.component')
          .then(m => m.ProvidersSettingsComponent)
      },
      {
        path: 'integrations',
        loadComponent: () => import('@mnema/settings/settings/_components/integrations-settings/integrations-settings.component')
          .then(m => m.IntegrationsSettingsComponent)
      },
      {
        path: 'server',
        loadComponent: () => import('@mnema/settings/settings/_components/server-settings/server-settings.component')
          .then(m => m.ServerSettingsComponent)
      },
      {
        path: 'releases',
        loadComponent: () => import('@mnema/settings/settings/_components/release-browser/release-browser.component')
          .then(m => m.ReleaseBrowserComponent)
      },
    ]
  },
];
