import {ChangeDetectionStrategy, Component, inject, model, TemplateRef} from '@angular/core';
import {translate, TranslocoDirective} from '@jsverse/transloco';
import {Subject} from 'rxjs';
//import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {NgTemplateOutlet} from '@angular/common';
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";

@Component({
  selector: 'app-confirm-modal',
  imports: [
    TranslocoDirective,
    NgTemplateOutlet
  ],
  templateUrl: './confirm-modal.component.html',
  styleUrl: './confirm-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmModalComponent {

  private readonly modal = inject(NgbActiveModal);

  title = model<string>(translate('confirm-modal.title'));
  question = model.required<string>();
  bodyTemplate = model<TemplateRef<any> | null>(null);
  templateData = model<any>();

  private result = new Subject<boolean>();
  result$ = this.result.asObservable();

  confirm() {
    this.result.next(true);
    this.modal.close();
  }

  close() {
    this.result.next(false);
    this.modal.close();
  }

}
