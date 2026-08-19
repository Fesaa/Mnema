import {Pipe, PipeTransform} from '@angular/core';
import {ImportScanStatus} from "@mnema/features/import-scans/models";
import {translate} from "@jsverse/transloco";

@Pipe({
  name: 'importScanStatus',
})
export class ImportScanStatusPipe implements PipeTransform {

  transform(value: ImportScanStatus): string {
    switch (value) {
      case ImportScanStatus.Queued:
        return translate('import-scan-status-pipe.Queued');
      case ImportScanStatus.Started:
        return translate('import-scan-status-pipe.Started');
      case ImportScanStatus.Finished:
        return translate('import-scan-status-pipe.Finished');
      case ImportScanStatus.Failed:
        return translate('import-scan-status-pipe.Failed');

    }
  }

}
