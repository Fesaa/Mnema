import {Injectable} from '@angular/core';
import {environment} from "../../environments/environment";
import {HttpClient} from "@angular/common/http";
import {Preferences} from "../_models/preferences";

@Injectable({
  providedIn: 'root'
})
export class PreferencesService {

  baseUrl = environment.apiUrl + 'preferences';

  constructor(private httpClient: HttpClient) {
  }

  get() {
    return this.httpClient.get<Preferences>(this.baseUrl);
  }

  save(preference: Preferences) {
    return this.httpClient.post(this.baseUrl + '/', preference)
  }
}
