import {ChangeDetectionStrategy, Component, computed, HostListener, inject, OnInit, signal} from '@angular/core';
import {toSignal} from "@angular/core/rxjs-interop";
import {Breakpoint, UtilityService} from "../_services/utility.service";
import {ActivatedRoute, RouterLink} from "@angular/router";
import {AccountService} from "../_services/account.service";
import {NavService} from "../_services/nav.service";
import {NotificationService} from "../_services/notification.service";
import {Button, ButtonGroup, ButtonGroupService} from "../button-grid/button-group.service";
import {translate, TranslocoPipe} from "@jsverse/transloco";
import {TitleCasePipe} from "@angular/common";
import {animate, style, transition, trigger} from "@angular/animations";
import {MobileGridComponent} from "../button-grid/mobile-grid/mobile-grid.component";

@Component({
  selector: 'app-nav-header',
  templateUrl: './nav-header.component.html',
  styleUrls: ['./nav-header.component.scss'],
  imports: [
    RouterLink,
    TitleCasePipe,
    MobileGridComponent,
    TranslocoPipe
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
    ])
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavHeaderComponent implements OnInit {

  private readonly route = inject(ActivatedRoute);
  private readonly accountService = inject(AccountService);
  protected readonly navService = inject(NavService);
  protected readonly notificationService = inject(NotificationService);
  protected readonly buttonGroupService = inject(ButtonGroupService);
  protected readonly utilityService = inject(UtilityService);

  currentUser = this.accountService.currentUser;
  showNav = toSignal(this.navService.showNav$, {initialValue: false});

  isMobileGridOpen = signal(false);
  isAccountDropdownOpen = signal(false);

  isMobile = computed(() => this.showNav() && this.utilityService.breakPoint() <= Breakpoint.Mobile);
  isDesktop = computed(() => this.showNav() && this.utilityService.breakPoint() > Breakpoint.Mobile);

  mobileButtonGroups = computed<ButtonGroup[]>(() => [
    {
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

  navPageButtons = computed<Button[]>(() => {
    return [
      {
        title: translate('nav-bar.home'),
        icon: 'fa fa-home',
        navUrl: 'home'
      },
      ...this.buttonGroupService.pageGroup().buttons
    ];
  });

  navActionButtons = computed<Button[]>(() => {
    const buttons = [...this.buttonGroupService.actionGroup().buttons];
    // Find logout button to insert settings before it
    const logoutIndex = buttons.findIndex(b => b.onClick && b.onClick.toString().includes('logout'));

    const settingsButton: Button = {
      title: translate('button-groups.settings.title'),
      icon: 'fa fa-cog',
      navUrl: 'settings',
      standAlone: true,
    };

    if (logoutIndex !== -1) {
      buttons.splice(logoutIndex, 0, settingsButton);
    } else {
      buttons.push(settingsButton);
    }

    return buttons;
  });

  severity = computed((): 'info' | 'warn' | 'danger' => {
    const count = this.notificationService.notificationsCount();
    if (count < 4) return 'info';
    if (count < 10) return 'warn';
    return 'danger';
  });

  constructor() {
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const index = params['index'];
      if (index) {
        // Not used here, preserved for logic continuity
      }
    });
  }

  logout() {
    this.accountService.logout();
  }

  toggleMobileGrid() {
    this.isMobileGridOpen.update(v => !v);
  }

  toggleAccountDropdown() {
    this.isAccountDropdownOpen.update(v => !v);
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
