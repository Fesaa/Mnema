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
        return translate('provider-name-pipe.Nyaa');
      case Provider.MANGADEX:
        return translate('provider-name-pipe.Mangadex');
      case Provider.DYNASTY:
        return translate('provider-name-pipe.Dynasty');
      case Provider.WEBTOON:
        return translate('provider-name-pipe.Webtoons');
      case Provider.BATO:
        return translate('provider-name-pipe.Bato');
      case Provider.WEEBDEX:
        return "Weebdex"
      case Provider.COMIX:
        return "Comix"
      case Provider.KAGANE:
        return "Kagane"
      case Provider.Madokami:
        return "Madokami"
      case Provider.AthreaScans:
        return "Athrea Scans"
      default:
        return "Unknown";
    }
  }

}
