import {ChangeDetectionStrategy, Component, computed, HostListener, inject, signal} from '@angular/core';
import {toSignal} from "@angular/core/rxjs-interop";
import {Breakpoint, UtilityService} from "../_services/utility.service";
import {RouterLink} from "@angular/router";
import {AccountService} from "../_services/account.service";
import {NavService} from "../_services/nav.service";
import {NotificationService} from "../_services/notification.service";
import {ButtonGroup, ButtonGroupKey, ButtonGroupService} from "../button-grid/button-group.service";
import {translate, TranslocoPipe} from "@jsverse/transloco";
import {TitleCasePipe} from "@angular/common";
import {animate, style, transition, trigger} from "@angular/animations";
import {MobileGridComponent} from "../button-grid/mobile-grid/mobile-grid.component";
import {BadgeComponent} from "@mnema/shared/_component/badge/badge.component";

@Component({
  selector: 'app-nav-header',
  templateUrl: './nav-header.component.html',
  styleUrls: ['./nav-header.component.scss'],
  imports: [
    RouterLink,
    TitleCasePipe,
    MobileGridComponent,
    TranslocoPipe,
    BadgeComponent
  ],
  animations: [
    trigger('dropdownAnimation', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(-8px)' }),
        animate('150ms ease-out', style({ opacity: 1, transform: 'translateY(0)' })),
      ]),
      transition(':leave', [
        animate('100ms ease-in', style({ opacity: 0, transform: 'translateY(-8px)' })),
      ]),
    ]),
    trigger('expandCollapse', [
      transition(':enter', [
        style({ height: '0', opacity: 0, overflow: 'hidden' }),
        animate('200ms ease-out', style({ height: '*', opacity: 1 })),
      ]),
      transition(':leave', [
        style({ height: '*', opacity: 1, overflow: 'hidden' }),
        animate('200ms ease-in', style({ height: '0', opacity: 0 })),
      ]),
    ])
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavHeaderComponent {

  private readonly accountService = inject(AccountService);
  protected readonly navService = inject(NavService);
  protected readonly notificationService = inject(NotificationService);
  protected readonly buttonGroupService = inject(ButtonGroupService);
  protected readonly utilityService = inject(UtilityService);

  currentUser = this.accountService.currentUser;
  showNav = toSignal(this.navService.showNav$, {initialValue: false});

  isMobileGridOpen = signal(false);
  isAccountDropdownOpen = signal(false);
  expandedGroup = signal<ButtonGroupKey | null>(ButtonGroupKey.Actions);

  isMobile = computed(() => this.showNav() && this.utilityService.breakPoint() <= Breakpoint.Mobile);
  isDesktop = computed(() => this.showNav() && this.utilityService.breakPoint() > Breakpoint.Mobile);

  dashboardGroups = this.buttonGroupService.dashboardGroups;

  mobileButtonGroups = computed<ButtonGroup[]>(() => [
    {
      key: ButtonGroupKey.Any,
      title: '',
      icon: '',
      buttons: [
        {
          title: translate('nav-bar.home'),
          icon: 'fa fa-home',
          navUrl: 'home',
          standAlone: true,
        }
      ]
    },
    ...this.buttonGroupService.dashboardGroups(),
  ])

  toggleMobileGrid() {
    this.isMobileGridOpen.update(v => !v);
  }

  toggleGroup(key: ButtonGroupKey) {
    const cur = this.expandedGroup();
    if (cur === key) {
      this.expandedGroup.set(null);
    } else {
      this.expandedGroup.set(key);
    }
  }

  isGroupExpanded(key: ButtonGroupKey): boolean {
    return this.expandedGroup() == key;
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (
      this.isAccountDropdownOpen() &&
      !target.closest('.account-dropdown') &&
      !target.closest('.account-toggle')
    ) {
      this.isAccountDropdownOpen.set(false);
    }
  }

}
