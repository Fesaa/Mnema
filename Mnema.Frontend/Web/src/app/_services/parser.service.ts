import {inject, Injectable} from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import {Observable} from 'rxjs';
import {environment} from '@env/environment';
import {ParseResult} from '@mnema/_models/parse-result';
import {ContentFormat} from '@mnema/features/monitored-series/monitored-series.service';

@Injectable({
  providedIn: 'root'
})
export class ParserService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + 'Parser';

  testParse(query: string, contentFormat: ContentFormat): Observable<ParseResult> {
    const params = new HttpParams()
      .set('query', query)
      .set('contentFormat', ContentFormat[contentFormat]);

    return this.httpClient.get<ParseResult>(this.baseUrl, {params});
  }
}
