import { ResolveFn } from '@angular/router';
import {ImportScan} from "@mnema/features/import-scans/models";
import {inject} from "@angular/core";
import {ImportScanService} from "@mnema/features/import-scans/import-scan.service";

export const importScanResolver: ResolveFn<ImportScan> = (route, state) => {
  const importScanService = inject(ImportScanService);

  return importScanService.getById(route.paramMap.get('id')!);
};
