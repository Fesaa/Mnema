import {Component, computed, inject, input, output} from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import {DirectoryImportResult, DirectoryImportStatus} from "@mnema/features/import-scans/models";
import {ImportScanService} from "@mnema/features/import-scans/import-scan.service";
import {ModalService} from "@mnema/_services/modal.service";
import {tap} from "rxjs";

@Component({
  selector: 'app-directory-import-scan-result-overview',
  imports: [TranslocoDirective],
  templateUrl: './directory-import-scan-result-overview.component.html',
  styleUrl: './directory-import-scan-result-overview.component.scss',
})
export class DirectoryImportScanResultOverviewComponent {

  private readonly importScanService = inject(ImportScanService);
  private readonly modalService = inject(ModalService);

  result = input.required<DirectoryImportResult>();

  canAutoAccept = computed(() => this.result().parsedHardcoverId != 0 || this.result().parsedMangaBakaId != 0)

  decisionResult = output<DirectoryImportStatus>();

  /** Opens the (future) modal for linking a metadata provider to this result. */
  linkMetadataProvider() {

  }

  /** Removes the item from the queue entirely. Does nothing further. */
  reject() {
    this.importScanService.rejectDirectoryImportResult(this.result().id).pipe(
      tap(() => this.decisionResult.emit(DirectoryImportStatus.Rejected))
    ).subscribe();
  }

  /** Pushes the item to the end of the queue (max sort value) without removing it. */
  skip() {
    this.importScanService.skipDirectoryImportResult(this.result().id).pipe(
      tap(() => this.decisionResult.emit(DirectoryImportStatus.Rejected))
    ).subscribe();
  }

  /** Opens the "create monitored series" modal, pre-filled from this result. */
  accept() {

  }

  /** No confirmation - lets Mnema generate the monitored series automatically. */
  autoAccept() {

  }

}
