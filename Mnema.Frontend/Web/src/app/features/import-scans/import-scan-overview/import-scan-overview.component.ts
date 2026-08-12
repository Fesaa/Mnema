import {Component, effect, inject, input, signal} from '@angular/core';
import {DirectoryImportResult, ImportError, ImportScan} from "@mnema/features/import-scans/models";
import {TranslocoDirective} from "@jsverse/transloco";
import {ImportScanService} from "@mnema/features/import-scans/import-scan.service";
import {PaginatorComponent} from "@mnema/shared/_component/paginator/paginator.component";
import {Observable} from "rxjs";
import {PagedList} from "@mnema/_models/paged-list";
import {
  DirectoryImportScanResultOverviewComponent
} from "@mnema/features/import-scans/import-scan-overview/directory-import-scan-result-overview/directory-import-scan-result-overview.component";

@Component({
  selector: 'app-import-scan-overview',
  imports: [
    TranslocoDirective,
    PaginatorComponent,
    DirectoryImportScanResultOverviewComponent
  ],
  templateUrl: './import-scan-overview.component.html',
  styleUrl: './import-scan-overview.component.scss',
})
export class ImportScanOverviewComponent {

  private readonly importScanService = inject(ImportScanService);

  scan = input.required<ImportScan>();

  mode = signal<'directories' | 'errors'>('directories');

  pageLoader = (pageNumber: number, pageSize: number): Observable<PagedList<any>> => {
    if (this.mode() === 'directories') {
      return this.importScanService.getDirectoriesPaged(this.scan().id, pageNumber, pageSize);
    }

    return this.importScanService.getErrorsPaged(this.scan().id, pageNumber, pageSize);
  }

  selectedItem = signal<DirectoryImportResult | ImportError | null>(null);

  constructor() {
    effect(() => {
      this.mode(); // Mode changes => reset selection
      this.selectedItem.set(null);
    });
  }

  selectItem(item: DirectoryImportResult | ImportError) {
    this.selectedItem.set(item);
  }

}
