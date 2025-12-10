import {Injectable} from "@angular/core";
import {Translation, TranslocoLoader} from "@jsverse/transloco";
import {HttpClient} from "@angular/common/http";

@Injectable({ providedIn: 'root' })
export class TranslocoLoaderImpl implements TranslocoLoader {

  constructor(private httpClient: HttpClient) {
  }

  getTranslation(lang: string) {
    return this.httpClient.get<Translation>(`./assets/i18n/${lang}.json`);
  }
}
