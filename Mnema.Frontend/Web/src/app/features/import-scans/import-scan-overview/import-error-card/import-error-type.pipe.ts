import { Pipe, PipeTransform } from '@angular/core';
import {ImportErrorType} from "@mnema/features/import-scans/models";
import {translate} from "@jsverse/transloco";

@Pipe({
  name: 'importErrorType',
})
export class ImportErrorTypePipe implements PipeTransform {

  transform(value: ImportErrorType): string {
    switch (value) {
      case ImportErrorType.UnknownDirectory:
        return translate('import-error-type-pipe.unknown-directory');
      case ImportErrorType.GenericException:
        return translate('import-error-type-pipe.generic-exception');
      case ImportErrorType.MixedContentFormats:
        return translate('import-error-type-pipe.mixed-content-formats');
      case ImportErrorType.FailedToParseContentFormat:
        return translate('import-error-type-pipe.failed-to-parse-content-format');
      case ImportErrorType.FailedToParseSeries:
        return translate('import-error-type-pipe.failed-to-parse-series');

    }
  }

}
