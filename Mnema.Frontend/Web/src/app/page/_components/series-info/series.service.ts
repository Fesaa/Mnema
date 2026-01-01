import {inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {environment} from "../../../../environments/environment";
import {Provider} from "../../../_models/page";
import {Series} from "./_types";

@Injectable({
  providedIn: 'root',
})
export class SeriesService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  public getSeriesInfo(provider: Provider, id: string) {
    return this.httpClient.get<Series>(this.baseUrl + `Content/series-info?provider=${provider}&id=${id}`);
  }

}
