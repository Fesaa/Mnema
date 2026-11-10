import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {ActivatedRoute, Router, RouterOutlet} from '@angular/router';
import {NavService} from '../../_services/nav.service';

const LEGACY_FRAGMENTS: Record<string, string> = {
  preferences: 'preferences',
  pages: 'pages',
  server: 'server',
  releases: 'releases',
};

@Component({
  selector: 'app-settings',
  imports: [
    RouterOutlet,
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsComponent {

  private readonly navService = inject(NavService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  constructor() {
    this.navService.setNavVisibility(true);

    const fragment = this.route.snapshot.fragment;
    const target = fragment ? LEGACY_FRAGMENTS[fragment] : undefined;

    if (target) {
      this.router.navigate(['/settings', target], {replaceUrl: true})
        .catch(err => console.error(err));
    }
  }
}
