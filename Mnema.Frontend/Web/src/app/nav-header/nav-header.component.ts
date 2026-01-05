import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  computed, effect, ElementRef,
  HostListener, inject,
  OnInit,
  signal, ViewChild
} from '@angular/core';
import {toSignal} from "@angular/core/rxjs-interop";
import {Breakpoint, UtilityService} from "../_services/utility.service";
import {PageService} from "../_services/page.service";
import {ActivatedRoute, RouterLink} from "@angular/router";
import {AccountService} from "../_services/account.service";
import {NavService} from "../_services/nav.service";
import {NotificationService} from "../_services/notification.service";
import {EventType, SignalRService} from "../_services/signal-r.service";
import {ButtonGroupService, Button, ButtonGroup} from "../button-grid/button-group.service";
import {translate, TranslocoService} from "@jsverse/transloco";
import {Role, User} from "../_models/user";
import {Page, Provider} from "../_models/page";
import {AsyncPipe, TitleCasePipe} from "@angular/common";
import {animate, style, transition, trigger} from "@angular/animations";
import {catchError, filter, fromEvent, of, take, tap, timeout} from "rxjs";
import {ButtonGridComponent} from "../button-grid/button-grid.component";
import {TranslocoDirective, TranslocoPipe} from "@jsverse/transloco";

interface NavItem {
  label: string;
  icon?: string;
  routerLink?: string;
  queryParams?: Record<string, any>;
  command?: () => void;
  roles?: Role[];
}

const drawerAnimation = trigger('drawerAnimation', [
  transition(':enter', [
    style({ transform: 'translateY(100%)', opacity: 0 }),
    animate('250ms ease-out', style({ transform: 'translateY(0)', opacity: 1 })),
  ]),
  transition(':leave', [
    animate('200ms ease-in', style({ transform: 'translateY(100%)', opacity: 0 })),
  ]),
]);

const dropdownAnimation = trigger('dropdownAnimation', [
  transition(':enter', [
    style({ opacity: 0, transform: 'translateY(-8px)' }),
    animate('150ms ease-out', style({ opacity: 1, transform: 'translateY(0)' })),
  ]),
  transition(':leave', [
    animate('100ms ease-in', style({ opacity: 0, transform: 'translateY(-8px)' })),
  ]),
]);



const fadeAnimation = trigger('fadeAnimation', [
  transition(':enter', [
    style({ opacity: 0 }),
    animate('200ms ease-out', style({ opacity: 1 })),
  ]),
  transition(':leave', [
    animate('150ms ease-in', style({ opacity: 0 })),
  ]),
]);

@Component({
  selector: 'app-nav-header',
  templateUrl: './nav-header.component.html',
  styleUrls: ['./nav-header.component.scss'],
  imports: [
    RouterLink,
    TitleCasePipe,
    ButtonGridComponent,
    TranslocoPipe
  ],
  animations: [drawerAnimation, dropdownAnimation, fadeAnimation],
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
