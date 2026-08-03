import {Component, computed, effect, inject, linkedSignal, signal} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {NavService} from '../../_services/nav.service';
import {PreferenceSettingsComponent} from "./_components/preference-settings/preference-settings.component";
import {PagesSettingsComponent} from "./_components/pages-settings/pages-settings.component";
import {ServerSettingsComponent} from "./_components/server-settings/server-settings.component";
import {
  Button,
  ButtonGroupService,
  SettingsID
} from "../../button-grid/button-group.service";
import {ReleaseBrowserComponent} from "@mnema/settings/settings/_components/release-browser/release-browser.component";

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    PreferenceSettingsComponent,
    PagesSettingsComponent,
    ServerSettingsComponent,
    ReleaseBrowserComponent,
  ],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent {

  private navService = inject(NavService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private readonly buttonGroupService = inject(ButtonGroupService);

  showMobileConfig = signal(false);

  readonly visibleSettings = computed(() => this.buttonGroupService.settingsGroup().buttons.filter(btn => btn.id));

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
