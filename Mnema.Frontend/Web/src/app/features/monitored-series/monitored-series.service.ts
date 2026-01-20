import {inject, Injectable} from '@angular/core';
import {HttpClient, HttpParams} from "@angular/common/http";
import {Observable, of, tap} from "rxjs";
import {environment} from "@env/environment";
import {FormDefinition} from "@mnema/generic-form/form";
import {Provider} from "@mnema/_models/page";
import {PagedList} from "@mnema/_models/paged-list";
import {Series} from "@mnema/page/_components/series-info/_types";

export type MonitoredSeries = {
  id: string;
  title: string;
  summary: string;
  coverUrl?: string;
  refUrl?: string;
  providers: Provider[];
  baseDir: string;
  format: Format;
  contentFormat: ContentFormat;
  hardcoverId: string,
  mangabakaId: string;
  titleOverride: string;
  validTitles: string[];
  lastDataRefreshUtc: string;
  chapters: MonitoredChapter[];
}

export enum Format {
  Archive = 0,
  Epub = 1,
}

export enum ContentFormat {
  Manga = 0,
  LightNovel = 1,
  Book = 2,
  Comic = 3,
}

export type MonitoredChapter = {
  id: string;
  externalId: string;
  seriesId: string;
  status: MonitoredChapterStatus;
  title: string;
  summary: string;
  volume: string;
  chapter: string;
  coverUrl?: string;
  refUrl?: string;
  filePath?: string;
  releaseDate?: string;
}

export enum MonitoredChapterStatus {
  NotMonitored = 0,
  Missing,
  Upcoming,
  Importing,
  Available
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

  get(id: string) {
    return this.httpClient.get<MonitoredSeries>(`${this.baseUrl}/${id}`);
  }

  resolvedSeries(id: string) {
    return this.httpClient.get<Series>(`${this.baseUrl}/${id}/resolved-series`);
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
