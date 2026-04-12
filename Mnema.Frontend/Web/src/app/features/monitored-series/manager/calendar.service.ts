import {inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {environment} from "@env/environment";

@Injectable({
  providedIn: 'root',
})
export class CalendarService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getCalenderLink() {
    return this.httpClient.get(`${this.baseUrl}Calendar/url`, {responseType: 'text'});
  }

}
