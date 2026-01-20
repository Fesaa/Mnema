import {inject, Pipe, PipeTransform} from '@angular/core';
import {TranslocoService} from "@jsverse/transloco";
import {Format} from "@mnema/features/monitored-series/monitored-series.service";

@Pipe({
  name: 'format',
})
export class FormatPipe implements PipeTransform {

  private readonly transLoco = inject(TranslocoService);

  transform(value: Format): string {
    switch (value) {
      case Format.Archive:
        return this.transLoco.translate('format-pipe.archive');
      case Format.Epub:
        return this.transLoco.translate('format-pipe.epub');
    }
  }

}
