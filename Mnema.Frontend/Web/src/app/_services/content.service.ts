import {Injectable} from '@angular/core';
import {environment} from "../../environments/environment";
import {HttpClient} from "@angular/common/http";
import {map, Observable} from "rxjs";
import {StatsResponse} from "../_models/stats";
import {DownloadRequest, SearchRequest, StopRequest} from "../_models/search";
import {SearchInfo} from "../_models/Info";
import {ListContentData, Message, MessageType} from "../_models/messages";
import {Provider} from "../_models/page";

@Injectable({
  providedIn: 'root'
})
export class ContentService {

  baseUrl = environment.apiUrl + "content/";

  constructor(private httpClient: HttpClient) {
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

  search(req: SearchRequest): Observable<SearchInfo[]> {
    return this.httpClient.post<SearchInfo[]>(this.baseUrl + 'search', req)
  }

  download(req: DownloadRequest) {
    return this.httpClient.post(this.baseUrl + 'download', req);
  }

  stop(req: StopRequest) {
    return this.httpClient.post(this.baseUrl + 'stop', req)
  }

  infoStats() {
    return this.httpClient.get<StatsResponse>(this.baseUrl + 'stats')
  }

  private sendMessage<T, R>(msg: Message<T>): Observable<R | undefined> {
    return this.httpClient.post<Message<R>>(this.baseUrl + "message", msg).pipe(map(msg => msg.data))
  }
}
