import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {NotificationService} from "../_services/notification.service";
import {Notification} from "../_models/notifications";
import {ToastService} from "../_services/toast.service";
import {FormsModule, NonNullableFormBuilder, ReactiveFormsModule} from "@angular/forms";
import {NavService} from "../_services/nav.service";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {UtcToLocalTimePipe} from "../_pipes/utc-to-local.pipe";
import {TableComponent} from "../shared/_component/table/table.component";
import {ModalService} from "../_services/modal.service";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {NotificationInfoModalComponent} from "./_components/notification-info-modal/notification-info-modal.component";
import {DefaultModalOptions} from "../_models/default-modal-options";
import {Tracker} from "../shared/data-structures/tracker";

@Component({
  selector: 'app-notifications',
  imports: [
    FormsModule,
    TranslocoDirective,
    UtcToLocalTimePipe,
    TableComponent,
    NgbTooltip,
    ReactiveFormsModule,
  ],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss'
})
export class NotificationsComponent implements OnInit {

  private readonly modalService = inject(ModalService);
  protected readonly notificationService = inject(NotificationService);
  private readonly toastService = inject(ToastService);
  private readonly navService = inject(NavService);
  private readonly fb = inject(NonNullableFormBuilder);

  tracker = new Tracker<Notification, number>((n) => n.id);
  visibleItems = signal<Notification[]>([]);
  allSelected = computed(() => this.visibleItems().length > 0 && this.visibleItems().length === this.tracker.items().length);

  ngOnInit(): void {
    this.navService.setNavVisibility(true);
  }

  onItemsChange(items: Notification[]) {
    this.visibleItems.set(items);
    this.tracker.reset();
  }

  loadNotifications(pn: number, ps: number) {
    return this.notificationService.all(pn, ps);
  }

  toggleAll() {
    if (this.allSelected()) {
      this.tracker.reset();
    } else {
      this.tracker.addAll(this.visibleItems());
    }
  }

  toggleSelect(notification: Notification) {
    this.tracker.toggle(notification)
  }

  show(n: Notification) {
    const [_, component] = this.modalService.open(NotificationInfoModalComponent, DefaultModalOptions);
    component.notification.set(n);
  }

  markRead(notification: Notification) {
    this.notificationService.markAsRead(notification.id).subscribe(() => notification.read = true);
  }

  markUnRead(notification: Notification) {
    this.notificationService.markAsUnread(notification.id).subscribe(() => notification.read = false);
  }

  async readSelected() {
    const ids = this.tracker.ids();
    if (ids.length === 0) {
      this.toastService.warningLoco("notifications.toasts.no-selected");
      return;
    }

    if (!await this.modalService.confirm({
      question: translate('notifications.confirm-read-many', {amount: ids.length})
    })) {
      return;
    }

    this.notificationService.readMany(ids).subscribe(() => {
      this.tracker.items().forEach(n => n.read = true);
      this.tracker.reset();
    });
  }

  async deleteSelected() {
    if (this.tracker.ids().length === 0) {
      this.toastService.warningLoco("notifications.toasts.no-selected");
      return;
    }

    if (!await this.modalService.confirm({
      question: translate('notifications.confirm-delete-many', {amount: this.tracker.ids().length})
    })) {
      return;
    }

    this.notificationService.deleteMany(this.tracker.ids()).subscribe(() => this.tracker.reset());
  }

  async delete(notification: Notification) {
    if (!await this.modalService.confirm({
      question: translate('notifications.confirm-delete', {title: notification.title})
    })) {
      return;
    }

    this.notificationService.deleteNotification(notification.id).subscribe();
  }

  trackBy(idx: number, notification: Notification): string {
    return `${notification.id}`
  }

}
