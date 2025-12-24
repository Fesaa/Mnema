import {Pipe, PipeTransform} from '@angular/core';
import {Provider} from "../_models/page";

@Pipe({
  name: 'providerName'
})
export class ProviderNamePipe implements PipeTransform {

  transform(value: Provider): string {
    switch (value) {
      case Provider.NYAA:
        return "Nyaa";
      case Provider.MANGADEX:
        return "Mangadex";
      case Provider.DYNASTY:
        return "Dynasty";
      case Provider.WEBTOON:
        return "WebToon";
      case Provider.BATO:
        return "Bato";
      case Provider.MANGABUDDY:
        return "Manga buddy"
      default:
        return "Unknown";
    }
  }

}
