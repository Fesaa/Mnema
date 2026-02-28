import {inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {environment} from "@env/environment";
import {PagedList} from "../../_models/paged-list";
import {Series} from "../../page/_components/series-info/_types";

export enum MetadataProvider {
  Hardcover = 0,
  Mangabaka = 1,
  Upstream = 2,
}

export interface MetadataSearchResult extends Series {
  monitoredSeriesId: string;
}

@Injectable({
  providedIn: 'root',
})
export class MetadataService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + "Metadata";

  search(provider: MetadataProvider, query: string, pageNumber: number, pageSize: number) {
    return this.httpClient.get<PagedList<MetadataSearchResult>>(this.baseUrl + `/search?provider=${provider}&query=${query}&pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getSeriesById(provider: MetadataProvider, id: string) {
    return this.httpClient.get<Series>(this.baseUrl + `/get-series&provider=${provider}&externalId=${id}`);
  }

}
