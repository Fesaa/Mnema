import {Pipe, PipeTransform} from '@angular/core';
import {Provider} from "../_models/page";

@Pipe({
  name: 'subscriptionExternalUrl',
  standalone: true
})
export class SubscriptionExternalUrlPipe implements PipeTransform {

  transform(contentId: string, provider: Provider): string {
    switch (provider) {
      case Provider.MANGADEX:
        return "https://mangadex.org/title/" + contentId;
      case Provider.WEBTOON:
        return "https://www.webtoons.com" + contentId;
      case Provider.DYNASTY:
        return "https://dynasty-scans.com/" + contentId;
      case Provider.BATO:
        return "https://jto.to/title/" + contentId;
      case Provider.WEEBDEX:
        return "https://weebdex.org/title/" + contentId;
      case Provider.COMIX:
        return "https://comix.to/title/" + contentId;
      case Provider.KAGANE:
        return "https://kagane.to/series/" + contentId;
      case Provider.NYAA:
        return "https://nyaa.si/?f=0&c=0_0&q=" + contentId;
      default:
        throw new Error(`Unsupported provider: ${provider}`);
    }
  }

}
