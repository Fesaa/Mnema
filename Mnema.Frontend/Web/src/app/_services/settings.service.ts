import {effect, inject, Injectable, signal} from '@angular/core';
import {environment} from "../../environments/environment";
import {Config, UpdateServerSettings} from '../_models/config';
import {HttpClient} from "@angular/common/http";
import {map, tap} from "rxjs";
import {MetadataProvider} from "@mnema/features/monitored-series/metadata.service";
import {MetadataProviderSettings} from "@mnema/_models/metadata-provider-settings";

@Injectable({
  providedIn: 'root'
})
export class SettingsService {

  private readonly httpClient = inject(HttpClient);

  baseUrl = environment.apiUrl + 'Settings/';

  private _config = signal<Config | undefined>(undefined);
  public config = this._config.asReadonly();

  private _isServerSetup = signal(false);
  public isServerSetup = this._isServerSetup.asReadonly();

  private _isAuthenticated = signal(false);
  public isAuthenticated = this._isAuthenticated.asReadonly();

  checkServerSetup() {
    return this.httpClient.get(this.baseUrl + 'is-setup', { responseType: 'text' }).pipe(
      map(r => r === 'true'),
      tap(isSetup => this._isServerSetup.set(isSetup))
    );
  }

  checkIsAuthenticated() {
    return this.httpClient.get(this.baseUrl + 'is-authenticated', { responseType: 'text' }).pipe(
      map(r => r === 'true'),
      tap(isAuthenticated => this._isAuthenticated.set(isAuthenticated))
    );
  }

  getConfig() {
    return this.httpClient.get<Config>(`${this.baseUrl}`).pipe(tap((config: Config) => {
      this._config.set(config);
    }));
  }

  updateConfig(config: UpdateServerSettings) {
    return this.httpClient.post<Config>(`${this.baseUrl}`, config).pipe(tap(config => {
      this._config.set(config);
    }));
  }

  getMetadataSettings(metadataProvider: MetadataProvider) {
    return this.httpClient.get<MetadataProviderSettings>(this.baseUrl + 'metadata-provider-settings?metadataProvider=' + metadataProvider);
  }

  saveMetadataSettings(settings: MetadataProviderSettings) {
    return this.httpClient.post(this.baseUrl + 'metadata-provider-settings?metadataProvider', settings);
  }

  sortMetadataProviders(metadataProviders: MetadataProvider[]) {
    return this.httpClient.post(this.baseUrl + 'sort-metadata-providers', metadataProviders);
  }

  getMetadataProviderOrder() {
    return this.httpClient.get<MetadataProvider[]>(this.baseUrl + 'metadata-provider-order');
  }
}
