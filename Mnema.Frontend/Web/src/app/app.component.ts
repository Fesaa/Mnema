import {Component, HostListener, inject, OnInit} from '@angular/core';
import {RouterOutlet} from '@angular/router';
import {NavHeaderComponent} from "./nav-header/nav-header.component";
import {Title} from "@angular/platform-browser";
import {Event, EventType, SignalRService} from "./_services/signal-r.service";
import {Notification, NotificationColour} from "./_models/notifications";
import {ToastrService} from "ngx-toastr";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavHeaderComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'Media Provider';

  private readonly toastr = inject(ToastrService);
  private readonly titleService = inject(Title);
  private readonly signalR = inject(SignalRService);

  constructor() {
    this.titleService.setTitle(this.title);
  }

  ngOnInit(): void {
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


  @HostListener('window:resize')
  @HostListener('window:orientationchange')
  setDocHeight() {
    this.updateVh();
  }

  private updateVh(): void {
    // Sets a CSS variable for the actual device viewport height. Needed for mobile dev.
    const vh = window.innerHeight * 0.01;
    document.documentElement.style.setProperty('--vh', `${vh}px`);
  }
}
