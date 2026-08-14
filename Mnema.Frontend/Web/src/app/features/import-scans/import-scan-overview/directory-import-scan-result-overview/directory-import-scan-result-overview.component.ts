import {Component, computed, inject, input, model, output, TemplateRef, viewChild} from '@angular/core';
import {TranslocoDirective, TranslocoService} from '@jsverse/transloco';
import {
  DirectoryImportResult,
  DirectoryImportStatus,
  UpdateDirectoryImportResult
} from "@mnema/features/import-scans/models";
import {ImportScanService} from "@mnema/features/import-scans/import-scan.service";
import {ModalService} from "@mnema/_services/modal.service";
import {EMPTY, map, switchMap, tap} from "rxjs";
import {
  MetadataProvider,
  MetadataSearchResult,
  MetadataService
} from "@mnema/features/monitored-series/metadata.service";
import {PagedList} from "@mnema/_models/paged-list";
import {ListSelectModalComponent} from "@mnema/shared/_component/list-select-modal/list-select-modal.component";
import {SentenceCasePipe} from "@mnema/_pipes/sentence-case.pipe";
import {ToastService} from "@mnema/_services/toast.service";
import {RouterLink} from "@angular/router";

@Component({
  selector: 'app-directory-import-scan-result-overview',
  imports: [TranslocoDirective, SentenceCasePipe, RouterLink],
  templateUrl: './directory-import-scan-result-overview.component.html',
  styleUrl: './directory-import-scan-result-overview.component.scss',
})
export class DirectoryImportScanResultOverviewComponent {

  private readonly importScanService = inject(ImportScanService);
  private readonly modalService = inject(ModalService);
  private readonly toastR = inject(ToastService);
  private readonly metadataService = inject(MetadataService);
  private readonly transLoco = inject(TranslocoService);

  searchInfoTemplate = viewChild.required<TemplateRef<any>>('searchInfoPreview');

  result = model.required<DirectoryImportResult>();

  canAutoAccept = computed(() => this.result().parsedHardcoverId != 0 || this.result().parsedMangaBakaId != 0)

  decisionResult = output<DirectoryImportStatus>();

  reject() {
    this.importScanService.rejectDirectoryImportResult(this.result().id).pipe(
      tap(() => this.decisionResult.emit(DirectoryImportStatus.Rejected))
    ).subscribe();
  }

  skip() {
    this.importScanService.skipDirectoryImportResult(this.result().id).pipe(
      tap(() => this.decisionResult.emit(DirectoryImportStatus.Rejected))
    ).subscribe();
  }

  accept() {

  }

  autoAccept() {
    this.importScanService.autoAcceptDirectoryImportResult(this.result().id).pipe(
      tap(() => this.decisionResult.emit(DirectoryImportStatus.Imported))
    ).subscribe();
  }

  protected searchHardcover() {
    this.metadataService.search(MetadataProvider.Hardcover, this.result().parsedSeriesName, 0, 20).pipe(
      switchMap(results => this.promptForChoice(results)),
      map(sr => sr.id),
      switchMap(id => {
        const req: UpdateDirectoryImportResult = {
          parsedHardcoverId: parseInt(id),
          parsedMangaBakaId: this.result().parsedMangaBakaId,
          parsedSeriesName: this.result().parsedSeriesName,
        };
        return this.importScanService.updateDirectoryImportResult(this.result().id, req).pipe(
          map(() => req.parsedHardcoverId),
        );
      }),
      tap(id => {
        this.result.set(({
          ...this.result(),
          parsedHardcoverId: id,
        }));
      })
    ).subscribe();
  }

  protected searchMangabaka() {
    this.metadataService.search(MetadataProvider.Mangabaka, this.result().parsedSeriesName, 0, 20).pipe(
      switchMap(results => this.promptForChoice(results)),
      map(sr => sr.id),
      switchMap(id => {
        const req: UpdateDirectoryImportResult = {
          parsedMangaBakaId: parseInt(id),
          parsedHardcoverId: this.result().parsedHardcoverId,
          parsedSeriesName: this.result().parsedSeriesName,
        };
        return this.importScanService.updateDirectoryImportResult(this.result().id, req).pipe(
          map(() => req.parsedMangaBakaId),
        );
      }),
      tap(id => {
        this.result.set(({
          ...this.result(),
          parsedMangaBakaId: id,
        }));
      })
    ).subscribe();
  }

  private promptForChoice(results: PagedList<MetadataSearchResult>) {
    if (results.totalCount == 0) {
      this.toastR.warningLoco('monitored-series-detail.no-results');
      return EMPTY;
    }

    const [modal, component] = this.modalService.open(ListSelectModalComponent<MetadataSearchResult>, {
      size: "lg", centered: true
    });

    component.title.set(this.transLoco.translate('monitored-series-detail.select-search-result'));
    component.inputItems.set(results.items.map(sr => ({label: sr.title, value: sr, url: sr.refUrl ?? undefined})));
    component.itemsBeforeVirtual.set(8);
    component.requireConfirmation.set(true);
    component.itemTemplate.set(this.searchInfoTemplate());
    component.disableHover.set(true);

    return this.modalService.onClose$<MetadataSearchResult>(modal, true);
  }

}
