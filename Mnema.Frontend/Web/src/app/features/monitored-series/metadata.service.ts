import {inject, Injectable} from '@angular/core';
import {HttpClient, HttpParams} from "@angular/common/http";
import {environment} from "@env/environment";
import {PagedList} from "../../_models/paged-list";
import {Series} from "../../page/_components/series-info/_types";

export enum MetadataProvider {
  Hardcover = 0,
  Mangabaka = 1,
  Upstream = 3,
}

export const MetadataProviders = [MetadataProvider.Hardcover, MetadataProvider.Mangabaka, MetadataProvider.Upstream];

export interface MetadataSearchResult extends Series {
  monitoredSeriesId: string[];
}

export interface MangabakaCollection {
  id: string;
  seriesId: number;
  title?: string;
  type?: string;
  medium?: string;
  countMain: number;
  countExtra: number;
  countOther: number;
  languageIso?: string;
  publisherName?: string;
}

@Injectable({
  providedIn: 'root',
})
export class MetadataService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + "Metadata";

  search(provider: MetadataProvider, query: string, pageNumber: number, pageSize: number) {
    const httpParams = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize)
      .set('query', query)
      .set('provider', provider);


    return this.httpClient.get<PagedList<MetadataSearchResult>>(this.baseUrl + `/search`, {
      params: httpParams,
    });
  }

  getSeriesById(provider: MetadataProvider, id: string) {
    return this.httpClient.get<Series>(this.baseUrl + `/get-series&provider=${provider}&externalId=${id}`);
  }

  getCollections(externalId: string) {
    const httpParams = new HttpParams().set('externalId', externalId);

    return this.httpClient.get<MangabakaCollection[]>(this.baseUrl + `/mangabaka-collections`, {
      params: httpParams,
    });
  }

}
