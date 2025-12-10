import {Routes} from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    runGuardsAndResolvers: 'always',
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
      {path: '', pathMatch: 'full', redirectTo: 'home'},
      {
        path: '',
        loadChildren: () => import('./_routes/extra.routes').then(m => m.routes)
      }
    ]
  },
];
