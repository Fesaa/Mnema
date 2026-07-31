import {ChangeDetectionStrategy, Component, inject, model, signal} from '@angular/core';
import {TranslocoDirective} from "@jsverse/transloco";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {FormsModule} from "@angular/forms";
import {InfoStat} from "@mnema/_models/stats";
import {Provider} from "@mnema/_models/page";

@Component({
  selector: 'app-delete-download-modal',
  imports: [
    TranslocoDirective,
    FormsModule,
  ],
  templateUrl: './delete-download-modal.component.html',
  styleUrl: './delete-download-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeleteDownloadModalComponent {

  private readonly modal = inject(NgbActiveModal);

  info = model.required<InfoStat>();

  removeFromDownloadClient = signal(true);

  close() {
    this.modal.dismiss();
  }

  confirm() {
    this.modal.close(this.removeFromDownloadClient());
  }

  protected readonly Provider = Provider;
}
