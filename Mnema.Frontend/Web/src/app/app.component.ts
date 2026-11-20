import {Component, DestroyRef, HostListener, inject, OnInit} from '@angular/core';
import {NavigationEnd, Router, RouterOutlet} from '@angular/router';
import {NavHeaderComponent} from "./nav-header/nav-header.component";
import {Title} from "@angular/platform-browser";
import {Event, EventType, SignalRService} from "./_services/signal-r.service";
import {Notification, NotificationColour} from "./_models/notifications";
import {Breakpoint, UtilityService} from "./_services/utility.service";
import {translate, TranslocoDirective, TranslocoService} from "@jsverse/transloco";
import {takeUntilDestroyed, toSignal} from "@angular/core/rxjs-interop";
import {filter, map, tap} from "rxjs";
import {ActiveDownloadsService} from "./dashboard/active-downloads/active-downloads.service";
import {NotificationService} from "./_services/notification.service";
import {NavService} from "@mnema/_services/nav.service";
import {RetoastService} from "ngx-retoast";
import {CommandPaletteService} from './command-palette/command-palette.service';

const DOUBLE_SHIFT_WINDOW_MS = 300;

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavHeaderComponent, TranslocoDirective],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {

  private readonly toastr = inject(RetoastService);
  private readonly titleService = inject(Title);
  private readonly signalR = inject(SignalRService);
  protected readonly utilityService = inject(UtilityService);
  private readonly translocoService = inject(TranslocoService);
  private readonly destroyRef$ = inject(DestroyRef);
  private readonly activeDownloadService = inject(ActiveDownloadsService);
  private readonly notificationService = inject(NotificationService);
  private readonly navService = inject(NavService);
  private readonly router = inject(Router);
  private readonly commandPaletteService = inject(CommandPaletteService);
  private lastShiftAt = 0;

  readonly hideLayout = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => {
        let route = this.router.routerState.root;
        while (route.firstChild) {
          route = route.firstChild;
        }
        return route.snapshot.data['hideLayout'] === true;
      })
    ),
    { initialValue: false }
  );

  protected showNavBar = this.navService.showNav;

  ngOnInit() {
    this.translocoService.events$.pipe(
      takeUntilDestroyed(this.destroyRef$),
      tap(() => this.titleService.setTitle(translate('application.name')))
    ).subscribe();

    this.updateVh();

    this.signalR.events$.subscribe(event => {
      if (event.type !== EventType.Notification) return;

      const notification = (event as Event<Notification>).data;
      const title = this.stripHtml(notification.title);
      const summary = this.stripHtml(notification.summary);

      switch (notification.colour) {
        case NotificationColour.Primary:
          this.toastr.success(title, summary);
          break;
        case NotificationColour.Secondary:
          this.toastr.info(title, summary);
          break;
        case NotificationColour.Error:
          this.toastr.error(title, summary);
          break;
        case NotificationColour.Warn:
          this.toastr.warning(title, summary);
          break;
      }
    });
  }

  stripHtml(html: string): string {
    const tempDiv = document.createElement('div');
    tempDiv.innerHTML = html;
    return tempDiv.textContent || '';
  }

  @HostListener('window:visibilitychange')
  onUserReturn() {
    // Get some SignalR tracked things back in sync
    if (document.visibilityState === 'visible') {
      this.activeDownloadService.reload();
      this.notificationService.reload();
    }
  }


  @HostListener('window:keydown', ['$event'])
  onWindowKeydown(event: KeyboardEvent) {
    if (this.hideLayout() || event.isComposing) return;

    if (event.repeat) return;

    if (event.key !== 'Shift' || event.ctrlKey || event.altKey || event.metaKey) {
      this.lastShiftAt = 0;
      return;
    }

    const now = Date.now();
    if (now - this.lastShiftAt < DOUBLE_SHIFT_WINDOW_MS) {
      this.lastShiftAt = 0;
      this.commandPaletteService.open();
      return;
    }

    this.lastShiftAt = now;
  }

  @HostListener('window:resize')
  @HostListener('window:orientationchange')
  setDocHeight() {
    this.updateVh();
  }

  private updateVh(): void {
    // Sets a CSS variable for the actual device viewport height. Needed for mobile dev.
    const vh = window.innerHeight * 0.01;
    document.documentElement.style.setProperty('--vh', `${vh}px`);
    this.utilityService.updateBreakPoint();
  }

  protected readonly Breakpoint = Breakpoint;
}
