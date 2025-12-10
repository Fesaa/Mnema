import {Component, computed, effect, ElementRef, HostListener, inject, signal, ViewChild} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {NavService} from '../../_services/nav.service';
import {AccountService} from '../../_services/account.service';
import {Role, User} from '../../_models/user';
import {PreferenceSettingsComponent} from "./_components/preference-settings/preference-settings.component";
import {PagesSettingsComponent} from "./_components/pages-settings/pages-settings.component";
import {ServerSettingsComponent} from "./_components/server-settings/server-settings.component";
import {UserSettingsComponent} from "./_components/user-settings/user-settings.component";
import {TranslocoDirective} from "@jsverse/transloco";
import {AccountSettingsComponent} from "./_components/account-settings/account-settings.component";

export enum SettingsID {
  Account = "account",
  Server = "server",
  Preferences = "preferences",
  Pages = "pages",
  User = "user"
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
    UserSettingsComponent,
    TranslocoDirective,
    AccountSettingsComponent

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

  user = signal<User | null>(null);
  selected = signal<SettingsID>(SettingsID.Account);
  showMobileConfig = signal(false);

  readonly settings: SettingsTab[] = [
    { id: SettingsID.Account, title: "Account", icon: 'fa fa-user' },
    { id: SettingsID.Preferences, title: "Preferences", icon: 'fa fa-heart', roles: [Role.ManagePreferences] },
    { id: SettingsID.Pages, title: 'Pages', icon: 'fa fa-thumbtack', roles: [Role.ManagePages] },
    { id: SettingsID.Server, title: 'Server', icon: 'fa fa-server', roles: [Role.ManageServerConfigs] },
    { id: SettingsID.User, title: 'Users', icon: 'fa fa-users', roles: [Role.ManageUsers] },
  ];

  readonly visibleSettings = computed(() => {
    this.user(); // Compute when user changes

    return this.settings.filter(setting => this.canSee(setting.id));
  });

  constructor() {
    this.navService.setNavVisibility(true);

    effect(() => {
      const user = this.accountService.currentUser();
      if (!user) {
        this.router.navigateByUrl('/login');
        return;
      }
      this.user.set(user);

      if (!this.canSee(this.selected())) {
        this.selected.set(this.visibleSettings()[0].id);
      }
    });

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
