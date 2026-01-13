import {
  Component,
  computed,
  effect,
  inject,
  linkedSignal,
  signal
} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {NavService} from '../../_services/nav.service';
import {AccountService} from '../../_services/account.service';
import {PreferenceSettingsComponent} from "./_components/preference-settings/preference-settings.component";
import {PagesSettingsComponent} from "./_components/pages-settings/pages-settings.component";
import {ServerSettingsComponent} from "./_components/server-settings/server-settings.component";
import {TranslocoDirective} from "@jsverse/transloco";
import {
  ExternalConnectionSettingsComponent
} from "./_components/external-connection-settings/external-connection-settings.component";
import {Button, ButtonGroup, ButtonGroupService, SettingsID} from "../../button-grid/button-group.service";
import {MobileGridComponent} from "../../button-grid/mobile-grid/mobile-grid.component";
import {DownloadClientSettingsComponent} from "./_components/download-client/download-client-settings.component";

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    PreferenceSettingsComponent,
    PagesSettingsComponent,
    ServerSettingsComponent,
    TranslocoDirective,
    ExternalConnectionSettingsComponent,
    MobileGridComponent,
    DownloadClientSettingsComponent,
  ],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent {

  private navService = inject(NavService);
  private accountService = inject(AccountService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private readonly buttonGroupService = inject(ButtonGroupService);

  user = this.accountService.currentUser;
  showMobileConfig = signal(false);

  readonly visibleSettings = computed(() =>
    this.buttonGroupService.settingsGroup().buttons
      .filter(btn => btn.id && this.buttonGroupService.shouldRender(btn)));

  readonly mobileSettingsGroup = computed<ButtonGroup[]>(() => [
    {
      title: '',
      icon: '',
      buttons: this.visibleSettings().map(btn => ({
        ...btn,
        standAlone: true,
        onClick: () => {
          this.setSettings(btn.id as SettingsID);
          this.toggleMobile();
        },
      }))
    }
  ]);

  readonly selected = linkedSignal<Button[], SettingsID>({
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

  toggleMobile() {
    this.showMobileConfig.update(v => !v);
  }

  setSettings(id: SettingsID) {
    this.selected.set(id);
    this.showMobileConfig.set(false);
  }

  canSee(id: SettingsID) {
    return this.visibleSettings().some(s => s.id === id);
  }

  protected readonly SettingsID = SettingsID;
}
