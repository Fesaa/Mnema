import {inject, Injectable} from '@angular/core';
import {environment} from "../../environments/environment";
import {HttpClient, HttpParams} from "@angular/common/http";
import {Subscription} from "../_models/subscription";
import {Observable, of, tap} from "rxjs";
import {Provider} from "../_models/page";
import {PagedList} from "../_models/paged-list";
import {FormDefinition} from "../generic-form/form";

@Injectable({
  providedIn: 'root'
})
export class SubscriptionService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + "subscriptions";

  private _cachedForm?: FormDefinition;

  runOnce(id: string) {
    return this.httpClient.post(`${this.baseUrl}/run-once/${id}`, {}, {responseType: 'text'})
  }

  delete(id: string) {
    return this.httpClient.delete(`${this.baseUrl}/${id}`, {responseType: 'text'});
  }

  all(query: string, pageNumber: number, pageSize: number) {
    let params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    if (query) {
      params = params.set('query', query);
    }

    return this.httpClient.get<PagedList<Subscription>>(`${this.baseUrl}/all`, { params });
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

  getForm() {
    if (this._cachedForm) {
      return of(this._cachedForm);
    }

    return this.httpClient.get<FormDefinition>(`${this.baseUrl}/form`).pipe(
      tap(form => {
        if (environment.production) {
          this._cachedForm = form;
        }
      }),
    );
  }

}
