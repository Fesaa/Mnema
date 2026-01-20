import {inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {environment} from "@env/environment";
import {FormDefinition} from "@mnema/generic-form/form";
import {of, tap} from "rxjs";

@Injectable({
  providedIn: 'root',
})
export class FormService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + 'Form/'

  private cache = new Map<string, FormDefinition>();

  getMetadataProviderSettingsForm() {
    return this.getForm('metadata-provider-settings');
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
