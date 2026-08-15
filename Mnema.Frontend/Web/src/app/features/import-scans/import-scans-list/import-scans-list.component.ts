import {Component, EventEmitter, inject} from '@angular/core';
import {ImportScanService} from "@mnema/features/import-scans/import-scan.service";
import {TableComponent} from "@mnema/shared/_component/table/table.component";
import {ImportScan} from "@mnema/features/import-scans/models";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {ImportScanStatusPipe} from "@mnema/features/import-scans/import-scan-status.pipe";
import {UtcToLocalTimePipe} from "@mnema/_pipes/utc-to-local.pipe";
import {RouterLink} from "@angular/router";
import {ModalService} from "@mnema/_services/modal.service";
import {EMPTY, filter, switchMap, tap} from "rxjs";
import {Dir} from "@angular/cdk/bidi";
import {EventType, SignalRService} from "@mnema/_services/signal-r.service";

@Component({
  selector: 'app-import-scans-list',
  imports: [
    TableComponent,
    TranslocoDirective,
    ImportScanStatusPipe,
    UtcToLocalTimePipe,
    RouterLink
  ],
  templateUrl: './import-scans-list.component.html',
  styleUrl: './import-scans-list.component.scss',
})
export class ImportScansListComponent {

  private readonly importScansService = inject(ImportScanService);
  private readonly modalService = inject(ModalService);
  private readonly signalRService = inject(SignalRService);

  pageLoader = (pageNumber: number, pageSize: number) => {
    return this.importScansService.getPagedScans(pageNumber, pageSize);
  };

  reloader = new EventEmitter<void>();

  constructor() {
    this.signalRService.events$.pipe(
      filter(e => e.type === EventType.ScanFinished),
      tap(e => this.reloader.emit())
    ).subscribe();
  }

  trackById(index: number, item: ImportScan): string {
    return item.id;
  }

  delete(id: string) {
    this.modalService.confirm$({
      question: translate('import-scans-list.confirm-delete', {id: id}),
    }).pipe(
      filter(b => b),
      switchMap(() => this.importScansService.delete(id)),
      tap(() => this.reloader.emit())
    ).subscribe();
  }

  protected newScan() {
    this.modalService.getDirectory$('', {
      create: false, copy: false, showFiles: false, filter: true
    }).pipe(
      switchMap(directory => {
        if (!directory) return EMPTY;

        return this.importScansService.startScan({
          rootDir: directory,
        });
      }),
    ).subscribe();
  }
}
