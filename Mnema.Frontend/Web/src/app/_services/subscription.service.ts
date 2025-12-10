import {Injectable} from '@angular/core';
import {environment} from "../../environments/environment";
import {HttpClient} from "@angular/common/http";
import {Subscription} from "../_models/subscription";
import {Observable} from "rxjs";
import {Provider} from "../_models/page";

@Injectable({
  providedIn: 'root'
})
export class SubscriptionService {

  baseUrl = environment.apiUrl + "subscriptions";

  constructor(private httpClient: HttpClient) {
  }

  get(id: number): Observable<Subscription> {
    return this.httpClient.get<Subscription>(`${this.baseUrl}/${id}`);
  }

  runOnce(id: number) {
    return this.httpClient.post(`${this.baseUrl}/run-once/${id}`, {}, {responseType: 'text'})
  }

  runAll() {
    return this.httpClient.post(`${this.baseUrl}/run-all`, {});
  }

  delete(id: number) {
    return this.httpClient.delete(`${this.baseUrl}/${id}`, {responseType: 'text'});
  }

  all(): Observable<Subscription[]> {
    return this.httpClient.get<Subscription[]>(`${this.baseUrl}/all`);
  }

  new(s: Subscription): Observable<Subscription> {
    return this.httpClient.post<Subscription>(`${this.baseUrl}/new`, s);
  }

  update(s: Subscription) {
    return this.httpClient.post<Subscription>(`${this.baseUrl}/update`, s);
  }

  providers(): Observable<Provider[]> {
    return this.httpClient.get<Provider[]>(`${this.baseUrl}/providers`);
  }

}
