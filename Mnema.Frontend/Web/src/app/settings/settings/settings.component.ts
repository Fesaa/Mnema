import {
  Component,
  computed,
  effect,
  ElementRef,
  HostListener,
  inject,
  linkedSignal,
  signal,
  ViewChild
} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {NavService} from '../../_services/nav.service';
import {AccountService} from '../../_services/account.service';
import {Role, User} from '../../_models/user';
import {PreferenceSettingsComponent} from "./_components/preference-settings/preference-settings.component";
import {PagesSettingsComponent} from "./_components/pages-settings/pages-settings.component";
import {ServerSettingsComponent} from "./_components/server-settings/server-settings.component";
import {TranslocoDirective} from "@jsverse/transloco";
import {
  ExternalConnectionSettingsComponent
} from "./_components/external-connection-settings/external-connection-settings.component";

export enum SettingsID {
  Server = "server",
  Preferences = "preferences",
  Pages = "pages",
  ExternalConnections = "external_connections",
}

interface SettingsTab {
  id: SettingsID,
  title: string,
  icon: string,
  /**
   * Required roles to view this page, if empty everyone can view
   */
  roles?: Role[],
}

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    PreferenceSettingsComponent,
    PagesSettingsComponent,
    ServerSettingsComponent,
    TranslocoDirective,
    ExternalConnectionSettingsComponent,
  ],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent {
  private navService = inject(NavService);
  private accountService = inject(AccountService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  readonly SettingsID = SettingsID;

  @ViewChild('mobileConfig') mobileDrawerElement!: ElementRef<HTMLDivElement>;

  user = this.accountService.currentUser;
  showMobileConfig = signal(false);

  readonly settings: SettingsTab[] = [
    { id: SettingsID.Preferences, title: "Preferences", icon: 'fa fa-heart', roles: [] },
    { id: SettingsID.Pages, title: 'Pages', icon: 'fa fa-thumbtack', roles: [Role.ManagePages] },
    { id: SettingsID.Server, title: 'Server', icon: 'fa fa-server', roles: [Role.ManageServerConfigs] },
    { id: SettingsID.ExternalConnections, title: 'External Connections', icon: 'fa-solid fa-satellite-dish', roles: [Role.ManageExternalConnections] },
  ];

  readonly visibleSettings = computed(() => {
    this.user(); // Compute when user changes

    return this.settings.filter(setting => this.canSee(setting.id));
  });

  readonly selected = linkedSignal<SettingsTab[], SettingsID>({
    source: this.visibleSettings,
    computation: (newSettings, prev) => {
      if (newSettings.length === 0) return SettingsID.Preferences;

      const prevValue = prev?.value;
      if (!prevValue) return newSettings[0].id;

      if (newSettings.map(t => t.id).includes(prevValue)) {
        return prevValue;
      }

      return newSettings[0].id;
    }
  });

  constructor() {
    this.navService.setNavVisibility(true);

    this.route.fragment.subscribe(fragment => {
      if (fragment && Object.values(SettingsID).includes(fragment as SettingsID)) {
        this.selected.set(fragment as SettingsID);
      }
    });

    effect(() => {
      this.router.navigate([], { fragment: this.selected() });
    });
  }

  @HostListener('document:touchend', ['$event'])
  onDocumentClick(event: Event) {
    if (!this.showMobileConfig()) return;

    const clickedElement = event.target as Node;
    if (!this.mobileDrawerElement.nativeElement.contains(clickedElement)) {
      this.showMobileConfig.set(false);
    }
  }

  toggleMobile() {
    this.showMobileConfig.update(v => !v);
  }

  setSettings(id: SettingsID) {
    this.selected.set(id);
    this.showMobileConfig.set(false);
  }

  canSee(id: SettingsID): boolean {
    const user = this.user();
    if (!user) return false;

    const setting = this.settings.find(s => s.id === id);
    if (!setting) return false;

    if (!setting.roles || setting.roles.length === 0) {
      return true;
    }

    for (const role of setting.roles) {
      if (user.roles.includes(role)) {
        return true;
      }
    }

    return false;
  }

  isMobile(): boolean {
    return window.innerWidth <= 768;
  }
}
