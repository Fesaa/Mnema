import { Routes } from '@angular/router';
import { setupGuard } from './_guards/setup.guard';
import { authGuard } from './_guards/auth.guard';
import { setupCompleteGuard } from './_guards/setup-complete.guard';
import { loggedOutGuard } from './_guards/logged-out.guard';
import {InitialSetupComponent} from "@mnema/authentication/initial-setup/initial-setup.component";
import {LoginComponent} from "@mnema/authentication/login/login.component";
import {devOnlyGuard} from "@mnema/_guards/dev-only.guard";

export const routes: Routes = [
  {
    path: '',
    runGuardsAndResolvers: 'always',
    canActivate: [setupGuard, authGuard],
    children: [
      {
        path: 'home',
        loadChildren: () => import('./_routes/dashboard.routes').then(m => m.routes)
      },
      {
        path: 'page',
        loadChildren: () => import('./_routes/page.routes').then(m => m.routes)
      },
      {
        path: 'settings',
        loadChildren: () => import('./_routes/settings.routes').then(m => m.routes)
      },
      { path: '', pathMatch: 'full', redirectTo: 'home' },
      {
        path: '',
        loadChildren: () => import('./_routes/extra.routes').then(m => m.routes)
      },
    ]
  },
  {
    path: 'dev-tools',
    canActivate: [setupGuard, authGuard, devOnlyGuard],
    loadChildren: () => import('./_routes/dev-tools.routes').then(m => m.routes)
  },
  {
    path: 'initial-setup',
    canActivate: [setupCompleteGuard],
    component: InitialSetupComponent,
    data: { hideLayout: true }
  },
  {
    path: 'login',
    canActivate: [loggedOutGuard],
    component: LoginComponent,
    data: { hideLayout: true }
  },
];
