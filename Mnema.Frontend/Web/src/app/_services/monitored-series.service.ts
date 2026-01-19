import {inject, Injectable} from '@angular/core';
import {environment} from "../../environments/environment";
import {HttpClient, HttpParams} from "@angular/common/http";
import {Observable, of, tap} from "rxjs";
import {PagedList} from "../_models/paged-list";
import {FormDefinition} from "../generic-form/form";
import {Provider} from "../_models/page";
import {MetadataBag} from "../_models/search";

export type MonitoredSeries = {
  id: string;
  title: string;
  validTitles: string[];
  providers: Provider[];
  baseDir: string;
  contentFormat: number;
  format: number;
  metadata: MetadataBag;
}

@Injectable({
  providedIn: 'root'
})
export class MonitoredSeriesService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + "MonitoredSeries";

  private _cachedForm?: FormDefinition;

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

    return this.httpClient.get<PagedList<MonitoredSeries>>(`${this.baseUrl}/all`, { params });
  }

  new(s: MonitoredSeries): Observable<MonitoredSeries> {
    return this.httpClient.post<MonitoredSeries>(`${this.baseUrl}/new`, s);
  }

  update(s: MonitoredSeries) {
    return this.httpClient.post<MonitoredSeries>(`${this.baseUrl}/update`, s);
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
