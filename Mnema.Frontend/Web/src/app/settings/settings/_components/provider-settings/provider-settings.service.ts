import {inject, Injectable} from '@angular/core';
import {environment} from "@env/environment";
import {HttpClient} from "@angular/common/http";
import {Provider} from "@mnema/_models/page";
import {MetadataBag} from "@mnema/_models/search";

@Injectable({
  providedIn: 'root',
})
export class ProviderSettingsService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + 'ProviderSettings';

  get(provider: Provider) {
    return this.httpClient.get<MetadataBag>(this.baseUrl + `?provider=${provider}`);
  }

  update(provider: Provider, metadata: MetadataBag) {
    return this.httpClient.post(this.baseUrl + `?provider=${provider}`, metadata);
  }


}
