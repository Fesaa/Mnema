import {inject, Pipe, PipeTransform} from '@angular/core';
import {TranslocoService} from "@jsverse/transloco";
import {ContentFormat} from "@mnema/features/monitored-series/monitored-series.service";

@Pipe({
  name: 'contentFormat',
})
export class ContentFormatPipe implements PipeTransform {

  private readonly transLoco = inject(TranslocoService);

  transform(value: ContentFormat): string {
    switch (value) {
      case ContentFormat.Manga:
        return this.transLoco.translate('content-format-pipe.Manga')
      case ContentFormat.LightNovel:
        return this.transLoco.translate('content-format-pipe.LightNovel')
      case ContentFormat.Book:
        return this.transLoco.translate('content-format-pipe.Book')
      case ContentFormat.Comic:
        return this.transLoco.translate('content-format-pipe.Comic')
    }
  }

}
