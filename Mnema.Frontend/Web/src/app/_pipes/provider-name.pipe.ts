import {Pipe, PipeTransform} from '@angular/core';
import {Provider} from "../_models/page";
import {translate} from "@jsverse/transloco";

@Pipe({
  name: 'providerName'
})
export class ProviderNamePipe implements PipeTransform {

  transform(value: Provider): string {
    switch (value) {
      case Provider.NYAA:
        return translate('provider-name-pipe.nyaa');
      case Provider.MANGADEX:
        return translate('provider-name-pipe.mangadex');
      case Provider.DYNASTY:
        return translate('provider-name-pipe.dynasty');
      case Provider.WEBTOON:
        return translate('provider-name-pipe.webtoon');
      case Provider.BATO:
        return translate('provider-name-pipe.bato');
      case Provider.WEEBDEX:
        return "Weebdex"
      default:
        return "Unknown";
    }
  }

}
