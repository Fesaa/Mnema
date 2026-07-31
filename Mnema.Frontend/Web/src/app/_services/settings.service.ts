import {effect, inject, Injectable, signal} from '@angular/core';
import {environment} from "../../environments/environment";
import {Config} from '../_models/config';
import {HttpClient} from "@angular/common/http";
import {tap} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class SettingsService {

  private readonly httpClient = inject(HttpClient);

  baseUrl = environment.apiUrl + 'config/';

  private _config = signal<Config | undefined>(undefined);
  public config = this._config.asReadonly();

  constructor() {
    this.getConfig().subscribe();
  }

  getConfig() {
    return this.httpClient.get<Config>(`${this.baseUrl}`).pipe(tap((config: Config) => {
      this._config.set(config);
    }));
  }

  updateConfig(config: Config) {
    return this.httpClient.post(`${this.baseUrl}`, config).pipe(tap(() => {
      this._config.set(config);
    }));
  }
}
