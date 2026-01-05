import {computed, inject, Injectable, TemplateRef} from '@angular/core';
import {AccountService} from "../_services/account.service";
import {NavigationExtras, Router, UrlTree} from "@angular/router";
import {Role} from "../_models/user";
import {PageService} from "../_services/page.service";
import {translate, TranslocoService} from "@jsverse/transloco";
import {Breakpoint, UtilityService} from "../_services/utility.service";
import {ModalService} from "../_services/modal.service";
import {ListSelectModalComponent} from "../shared/_component/list-select-modal/list-select-modal.component";
import {filter, tap} from "rxjs";
import {NotificationService} from "../_services/notification.service";
import {toSignal} from "@angular/core/rxjs-interop";
import {ActiveDownloadsService} from "../dashboard/active-downloads/active-downloads.service";

export enum SettingsID {
  Server = "server",
  Preferences = "preferences",
  Pages = "pages",
  ExternalConnections = "external_connections",
}

export interface Button {
  id?: any;
  icon: string;
  title: string;
  navUrl?: string | UrlTree;
  navExtras?: NavigationExtras;
  onClick?: () => void;
  /**
   * Button will not be grouped on mobile
   */
  standAlone?: boolean;
  requiredRoles?: Role[];
  badge?: string;
}

export interface ButtonGroup {
  icon: string;
  title: string;
  buttons: Button[];
}

@Injectable({
  providedIn: 'root',
})
export class ButtonGroupService {

  private readonly notificationService = inject(NotificationService);
  private readonly accountService = inject(AccountService);
  private readonly activeDownloadsService = inject(ActiveDownloadsService);
  private readonly transloco = inject(TranslocoService);
  private readonly utilityService = inject(UtilityService);
  private readonly modalService = inject(ModalService);
  private readonly pageService = inject(PageService);
  private readonly router = inject(Router);

  translationReloaded = toSignal(this.transloco.events$.pipe(
    filter(event => event.type === 'translationLoadSuccess')
  ))

  pageGroup = computed<ButtonGroup>(() => {
    this.translationReloaded();

    return {
      title: translate('button-groups.pages.title'),
      icon: 'fa fa-thumbtack',
      buttons: this.pageService.pages().map<Button>(page => ({
        title: page.title,
        icon: `fa ${page.icon}`,
        navUrl: 'page',
        navExtras: { queryParams: { id: page.id } }
      })),
    };
  });

  actionGroup = computed<ButtonGroup>(() => {
    this.translationReloaded();

    return {
      title: translate('button-groups.actions.title'),
      icon: 'fa fa-exclamation',
      buttons: [
        {
          title: translate('button-groups.actions.subscriptions'),
          icon: 'fa fa-bell',
          requiredRoles: [Role.Subscriptions],
          navUrl: 'subscriptions',
          standAlone: true,
        },
        {
          title: translate('button-groups.actions.downloads'),
          icon: 'fa fa-download',
          requiredRoles: [Role.Subscriptions],
          navUrl: 'active-downloads',
          standAlone: true,
          badge: this.activeDownloadsService.items().length > 0
            ? `${this.activeDownloadsService.items().length}` : undefined,
        },
        {
          title: translate('button-groups.actions.notifications'),
          icon: 'fa fa-inbox',
          navUrl: 'notifications',
          badge: this.notificationService.notificationsCount() > 0
            ? `${this.notificationService.notificationsCount()}` : undefined,
        },
        {
          title: translate('button-groups.actions.audit-log'),
          icon: 'fa fa-user-secret',
          navUrl: 'audit-log',
        },
        {
          title: translate('button-groups.settings.logout'),
          icon: 'fa fa-user-minus',
          onClick: () => this.accountService.logout(),
        },
      ],
    };
  });

  settingsGroup = computed<ButtonGroup>(() => {
    this.translationReloaded();

    return {
      title: translate('button-groups.settings.title'),
      icon: 'fa fa-cogs',
      buttons: [
        {
          title: translate('button-groups.settings.preferences'),
          icon: 'fa fa-heart',
          navUrl: 'settings',
          navExtras: { fragment: SettingsID.Preferences },
          id: SettingsID.Preferences
        },
        {
          title: translate('button-groups.settings.pages'),
          icon: 'fa fa-thumbtack',
          navUrl: 'settings',
          navExtras: { fragment: SettingsID.Pages },
          id: SettingsID.Pages
        },
        {
          title: translate('button-groups.settings.server'),
          icon: 'fa fa-server',
          navUrl: 'settings',
          navExtras: { fragment: SettingsID.Server },
          id: SettingsID.Server
        },
        {
          title: translate('button-groups.settings.external-connections'),
          icon: 'fa fa-user-secret',
          navUrl: 'settings',
          navExtras: { fragment: SettingsID.ExternalConnections },
          id: SettingsID.ExternalConnections
        },
      ],
    };
  });

  dashboardGroups = computed<ButtonGroup[]>(() => [
    this.pageGroup(),
    this.actionGroup(),
    this.settingsGroup(),
  ]);

  anyVisible(buttons: Button[]) {
    return buttons.some(button => this.shouldRender(button));
  }

  shouldRender(button: Button) {
    const user = this.accountService.currentUser();
    if (!user) return false;

    if (button.requiredRoles === undefined || button.requiredRoles.length === 0) {
      return true;
    }

    return button.requiredRoles.some(role => user.roles.includes(role));
  }

  mobileMode = computed(() => this.utilityService.breakPoint() < Breakpoint.Desktop );

  groupedButtons(group: ButtonGroup) {
    return (this.mobileMode() ? group.buttons.filter(btn => !btn.standAlone) : [])
      .filter(btn => this.shouldRender(btn));
  }

  standAloneButtons(group: ButtonGroup) {
    return (this.mobileMode()
      ? group.buttons.filter(btn => !!btn.standAlone)
      : group.buttons)
      .filter(btn => this.shouldRender(btn));
  }

  groupBadge(group: ButtonGroup): string | undefined {
    const counts = this.groupedButtons(group)
      .map(btn => btn.badge)
      .filter(badge => !!badge)
      .map(badge => parseInt(badge!))
      .filter(num => !isNaN(num));

    if (counts.length === 0) return undefined;

    const total = counts.reduce((acc, curr) => acc + curr, 0);
    return total > 0 ? `${total}` : undefined;
  }

  handleButtonClick(button: Button, event?: Event): void {
    if (button.onClick) {
      event?.preventDefault();
      button.onClick();
    }

    if (button.navUrl) {
      event?.preventDefault();
      this.router.navigate([button.navUrl], button.navExtras)
        .catch(err => console.error(err));
    }
  }

  handleGroupClick(group: ButtonGroup, templ?: TemplateRef<any>): void {
    const [modal, component] = this.modalService.open(ListSelectModalComponent, {
      size: 'lg', centered: true,
    });

    component.title.set(group.title);
    component.inputItems.set(this.groupedButtons(group).map(btn => ({label: btn.title, value: btn})));
    component.showFooter.set(false);
    component.requireConfirmation.set(false);

    if (templ) {
      component.itemTemplate.set(templ);
    }

    this.modalService.onClose$<Button>(modal).pipe(
      tap(btn => this.handleButtonClick(btn))
    ).subscribe();
  }

}
