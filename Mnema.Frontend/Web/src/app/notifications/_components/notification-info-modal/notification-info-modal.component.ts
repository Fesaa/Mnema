import {ChangeDetectionStrategy, Component, inject, model} from '@angular/core';
import {Notification} from "../../../_models/notifications";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {SafeHtmlPipe} from "../../../_pipes/safe-html-pipe";
import {BadgeComponent} from "../../../shared/_component/badge/badge.component";
import {DatePipe} from "@angular/common";

@Component({
  selector: 'app-notification-info-modal',
  imports: [
    TranslocoDirective,
    SafeHtmlPipe,
    BadgeComponent,
    DatePipe
  ],
  templateUrl: './notification-info-modal.component.html',
  styleUrl: './notification-info-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotificationInfoModalComponent {

  private readonly modal = inject(NgbActiveModal);

  notification = model.required<Notification>();

  close() {
    this.modal.close();
  }

  get body() {
    let raw = this.notification().body;
    return raw ? raw.replace(/\n/g, '<br>') : '';
  }

  protected readonly translate = translate;
}
