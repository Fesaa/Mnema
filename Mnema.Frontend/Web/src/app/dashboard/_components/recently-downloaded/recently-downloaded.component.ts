import {Component, inject, OnInit, signal} from '@angular/core';
import {NotificationService} from '../../../_services/notification.service';
import {Notification} from "../../../_models/notifications";
import {TranslocoDirective} from "@jsverse/transloco";
import {UtcToLocalTimePipe} from "../../../_pipes/utc-to-local.pipe";
import {ToastService} from "../../../_services/toast.service";
import {BadgeComponent} from "../../../shared/_component/badge/badge.component";
import {SafeHtmlPipe} from "../../../_pipes/safe-html-pipe";
import {ModalService} from "../../../_services/modal.service";
import {
  NotificationInfoModalComponent
} from "../../../notifications/_components/notification-info-modal/notification-info-modal.component";
import {DefaultModalOptions} from "../../../_models/default-modal-options";

@Component({
  selector: 'app-recently-downloaded',
  imports: [
    TranslocoDirective,
    UtcToLocalTimePipe,
    BadgeComponent,
    SafeHtmlPipe,
  ],
  templateUrl: './recently-downloaded.component.html',
  styleUrl: './recently-downloaded.component.scss'
})
export class RecentlyDownloadedComponent implements OnInit{

  private readonly modalService = inject(ModalService);
  private readonly notificationService = inject(NotificationService);
  private readonly toastService = inject(ToastService);

  downloads = signal<Notification[]>([]);

  ngOnInit(): void {
    this.load();
  }

  private load() {
    this.notificationService.recent().subscribe((recent) => {
      this.downloads.set(recent);
    });
  }

  markRead(download: Notification) {
    this.notificationService.markAsRead(download.ID).subscribe({
      next: () => {
        this.downloads.update(x => x.filter(d => d.ID !== download.ID));
        this.load();
      },
      error: err => {
        this.toastService.genericError(err.error.message);
      }
    })
  }

  show(n: Notification) {
    const [_, component] = this.modalService.open(NotificationInfoModalComponent, DefaultModalOptions);
    component.notification.set(n);
  }

  formattedBody(notification: Notification) {
    let body = notification.body;
    body = body ? body.replace(/\n/g, '<br>') : '';
    return body;
  }

}
