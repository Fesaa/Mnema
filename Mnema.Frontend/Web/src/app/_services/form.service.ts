import {inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {environment} from "@env/environment";
import {FormDefinition} from "@mnema/generic-form/form";
import {of, tap} from "rxjs";
import {Provider} from "@mnema/_models/page";
import {MetadataProvider} from "@mnema/features/monitored-series/metadata.service";

@Injectable({
  providedIn: 'root',
})
export class FormService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + 'Form/'

  private cache = new Map<string, FormDefinition>();

  getMetadataProviderSettingsForm(metadataProvider: MetadataProvider) {
    return this.getForm('metadata-provider-settings?metadataProvider=' + metadataProvider);
  }

  getProviderSettingsForm(provider: Provider) {
    return this.getForm(`provider-settings?provider=${provider}`);
  }

  preferencesForm() {
    return this.getForm('preferences');
  }

  serverSettingsForm() {
    return this.getForm('server-settings');
  }

  private getForm(endpoint: string) {
    if (this.cache.has(endpoint)) {
      return of(this.cache.get(endpoint)!);
    }

    return this.httpClient.get<FormDefinition>(this.baseUrl + endpoint).pipe(
      tap((response: FormDefinition) => {
        this.cache.set(endpoint, response);
      })
    );
  }

}
