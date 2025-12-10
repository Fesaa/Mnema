import {Pipe, PipeTransform} from '@angular/core';
import {Provider} from "../_models/page";

@Pipe({
  name: 'providerName'
})
export class ProviderNamePipe implements PipeTransform {

  transform(value: Provider): string {
    switch (value) {
      case Provider.DYNASTY:
        return "Dynasty";
      case Provider.LIMETORRENTS:
        return "Limetorrents";
      case Provider.MANGADEX:
        return "Mangadex";
      case Provider.NYAA:
        return "Nyaa";
      case Provider.SUBSPLEASE:
        return "Subsplease";
      case Provider.YTS:
        return "YTS";
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
