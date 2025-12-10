import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  computed, effect, ElementRef,
  HostListener, inject,
  OnInit,
  signal, ViewChild
} from '@angular/core';
import {PageService} from "../_services/page.service";
import {ActivatedRoute, RouterLink} from "@angular/router";
import {AccountService} from "../_services/account.service";
import {NavService} from "../_services/nav.service";
import {NotificationService} from "../_services/notification.service";
import {EventType, SignalRService} from "../_services/signal-r.service";
import {TranslocoService} from "@jsverse/transloco";
import {User} from "../_models/user";
import {Page} from "../_models/page";
import {AsyncPipe, TitleCasePipe} from "@angular/common";
import {animate, style, transition, trigger} from "@angular/animations";
import {catchError, filter, fromEvent, of, take, tap, timeout} from "rxjs";

interface NavItem {
  label: string;
  icon?: string;
  routerLink?: string;
  queryParams?: Record<string, any>;
  command?: () => void;
}

const drawerAnimation = trigger('drawerAnimation', [
  transition(':enter', [
    style({ transform: 'translateX(-100%)', opacity: 0 }),
    animate('250ms ease-out', style({ transform: 'translateX(0)', opacity: 1 })),
  ]),
  transition(':leave', [
    animate('200ms ease-in', style({ transform: 'translateX(-100%)', opacity: 0 })),
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



@Component({
  selector: 'app-nav-header',
  templateUrl: './nav-header.component.html',
  styleUrls: ['./nav-header.component.scss'],
  imports: [
    RouterLink,
    AsyncPipe,
    TitleCasePipe
  ],
  animations: [drawerAnimation, dropdownAnimation],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavHeaderComponent implements OnInit {

  private readonly pageService = inject(PageService);
  private readonly route = inject(ActivatedRoute);
  private readonly cdRef = inject(ChangeDetectorRef);
  private readonly accountService = inject(AccountService);
  protected readonly navService = inject(NavService);
  private readonly notificationService = inject(NotificationService);
  private readonly signalR = inject(SignalRService);
  private readonly transLoco = inject(TranslocoService);

  @ViewChild('mobileDrawer') mobileDrawerElement!: ElementRef<HTMLDivElement>;

  notifications = signal(0);
  currentUser = this.accountService.currentUser;
  pageItems = signal<Page[]>([]);
  accountItems = signal<NavItem[]>([]);

  isMobileMenuOpen = signal(false);
  isAccountDropdownOpen = signal(false);

  severity = computed((): 'info' | 'warn' | 'danger' => {
    const count = this.notifications();
    if (count < 4) return 'info';
    if (count < 10) return 'warn';
    return 'danger';
  });

  constructor() {
    effect(() => {
      const user = this.currentUser();
      if (!user) return;

      this.transLoco.events$.pipe(
        filter(e => e.type === "translationLoadSuccess"),
        take(1),
        timeout(1000),
        catchError(() => of(null)),
        tap(() => {
          this.loadPages();
          this.setAccountItems(user);
        })
      ).subscribe();

      this.notificationService.amount().subscribe(amount => {
        this.notifications.set(amount);
      });
    });
    effect(() => {
      this.loadPages(); // Calls the pages effect
    });
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const index = params['index'];
      if (index) {
        // Not used here, preserved for logic continuity
      }
    });

    this.signalR.events$.subscribe(event => {
      if (event.type === EventType.NotificationAdd) {
        this.notifications.update(n => n + 1);
      }
      if (event.type === EventType.NotificationRead) {
        const amount: number = event.data.amount;
        this.notifications.update(n => Math.max(0, n - amount));
      }
    });
  }

  @HostListener('document:touchend', ['$event'])
  onDocumentClick(event: Event) {
    if (!this.isMobileMenuOpen()) return;

    const clickedElement = event.target as Node;
    if (!this.mobileDrawerElement.nativeElement.contains(clickedElement)) {
      this.isMobileMenuOpen.set(false);
    }
  }

  loadPages() {
    const pages = this.pageService.pages();
    this.pageItems.set([
      {
        title: this.transLoco.translate("nav-bar.home"),
        id: -1,
        icon: "fa-home",
        dirs: [],
        customRootDir: '',
        modifiers: [],
        providers: [],
        sortValue: -100,
      },
      ...pages
    ]);
  }

  setAccountItems(user: User) {
    const items: NavItem[] = [
      {
        label: this.transLoco.translate("nav-bar.subscriptions"),
        icon: "fa-bell",
        routerLink: "/subscriptions"
      },
      {
        label: this.transLoco.translate("nav-bar.notifications"),
        icon: "fa-inbox",
        routerLink: "/notifications"
      },
      {
        label: this.transLoco.translate("nav-bar.settings"),
        icon: "fa-cog",
        routerLink: "/settings"
      },
      {
        label: this.transLoco.translate("nav-bar.sign-out"),
        icon: "fa-user-minus",
        command: () => this.logout()
      }
    ];

    this.accountItems.set(items);
  }

  logout() {
    this.accountService.logout();
  }

  toggleMobileMenu() {
    this.isMobileMenuOpen.update(v => !v);
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
