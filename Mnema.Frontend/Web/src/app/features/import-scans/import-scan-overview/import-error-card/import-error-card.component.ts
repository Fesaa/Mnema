import {Component, computed, inject, input, output, signal} from '@angular/core';
import {ImportError, ImportErrorType} from "@mnema/features/import-scans/models";
import {UtcToLocalTimePipe} from "@mnema/_pipes/utc-to-local.pipe";
import {TranslocoDirective} from "@jsverse/transloco";
import {
  ImportErrorTypePipe
} from "@mnema/features/import-scans/import-scan-overview/import-error-card/import-error-type.pipe";
import {ModalService} from "@mnema/_services/modal.service";
import {ImportScanService} from "@mnema/features/import-scans/import-scan.service";
import {tap} from "rxjs";

@Component({
  selector: 'app-import-error-card',
  imports: [
    UtcToLocalTimePipe,
    TranslocoDirective,
    ImportErrorTypePipe
  ],
  templateUrl: './import-error-card.component.html',
  styleUrl: './import-error-card.component.scss',
})
export class ImportErrorCardComponent {

  private readonly modalService = inject(ModalService);
  private readonly importScanService = inject(ImportScanService);

  error = input.required<ImportError>();
  showStackTrace = signal(false);

  action = output<void>();

  private readonly noRetryTypes = new Set([ImportErrorType.UnknownDirectory]);
  private readonly folderActionTypes = new Set([
    ImportErrorType.MixedContentFormats,
    ImportErrorType.FailedToParseContentFormat,
    ImportErrorType.FailedToParseSeries,
  ]);

  canRetry = computed(() => !this.noRetryTypes.has(this.error().type));
  canOpenFolder = computed(() => this.folderActionTypes.has(this.error().type));

  protected openFolder() {
    this.modalService.getDirectory$(this.error().path, {
      filter: true, copy: true, showFiles: true, create: false,
    }).subscribe();
  }

  protected dismiss() {
    this.importScanService.dismissImportError(this.error().id).pipe(
     tap(() => this.action.emit())
    ).subscribe();
  }

  protected retry() {
    this.importScanService.retryImportError(this.error().id).pipe(
      tap(() => this.action.emit())
    ).subscribe();
  }

}
