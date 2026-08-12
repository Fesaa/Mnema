import {Routes} from "@angular/router";
import {ImportScansListComponent} from "@mnema/features/import-scans/import-scans-list/import-scans-list.component";
import {
  ImportScanOverviewComponent
} from "@mnema/features/import-scans/import-scan-overview/import-scan-overview.component";
import {importScanResolver} from "@mnema/features/import-scans/import-scan.resolver";


export const routes: Routes = [
  {
    path: '',
    component: ImportScansListComponent,
  },
  {
    path: ':id',
    component: ImportScanOverviewComponent,
    resolve: {
      scan: importScanResolver
    }
  }
]
