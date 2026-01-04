import {inject, Injectable} from '@angular/core';
import {environment} from "../../environments/environment";
import {HttpClient} from "@angular/common/http";
import {map, Observable, of, tap} from "rxjs";
import {InfoStat, StatsResponse} from "../_models/stats";
import {DownloadRequest, SearchRequest, StopRequest} from "../_models/search";
import {SearchInfo} from "../_models/Info";
import {ListContentData, Message, MessageType} from "../_models/messages";
import {Provider} from "../_models/page";
import {PagedList} from "../_models/paged-list";
import {FormDefinition} from "../generic-form/form";

@Injectable({
  providedIn: 'root'
})
export class ContentService {

  private readonly httpClient = inject(HttpClient);
  private baseUrl = environment.apiUrl + "Content/";

  private _cachedForm?: FormDefinition;

  getForm() {
    if (this._cachedForm) {
      return of(this._cachedForm);
    }

    return this.httpClient.get<FormDefinition>(`${this.baseUrl}form`).pipe(
      tap(form => {
        if (environment.production) {
          this._cachedForm = form;
        }
      }),
    );
  }

  startDownload(provider: Provider, contentId: string) {
    return this.sendMessage<void, undefined>({
      provider: provider,
      contentId: contentId,
      type: MessageType.StartDownload,
    })
  }

  setFilter(provider: Provider, contentId: string, selectedContent: string[]) {
    return this.sendMessage<string[], undefined>({
      provider: provider,
      contentId: contentId,
      type: MessageType.SetToDownload,
      data: selectedContent,
    })
  }

  listContent(provider: Provider, contentId: string): Observable<ListContentData[]> {
    return this.sendMessage<void, ListContentData[]>({
      provider: provider,
      contentId: contentId,
      type: MessageType.MessageListContent,
    }).pipe(map(list => list || []));
  }

  search(req: SearchRequest, pageNumber: number = 0, pageSize: number = 20) {
    return this.httpClient.post<PagedList<SearchInfo>>(this.baseUrl + `search?pageNumber=${pageNumber}&pageSize=${pageSize}`, req)
  }

  download(req: DownloadRequest) {
    return this.httpClient.post(this.baseUrl + 'download', req);
  }

  stop(req: StopRequest) {
    return this.httpClient.post(this.baseUrl + 'stop', req)
  }

  infoStats() {
    return this.httpClient.get<InfoStat[]>(this.baseUrl + 'stats')
  }

  private sendMessage<T, R>(msg: Message<T>): Observable<R | undefined> {
    return this.httpClient.post<Message<string>>(this.baseUrl + "message", msg).pipe(map(msg => {
      if (msg.data) {
        return JSON.parse(msg.data) as R;
      }

      return msg.data as R;
    }))
  }
}
