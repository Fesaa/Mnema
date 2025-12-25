import {effect, inject, Injectable, signal} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {environment} from "../../environments/environment";
import {DownloadMetadata, Page, Provider} from "../_models/page";
import {Observable, of, ReplaySubject, tap} from "rxjs";
import {AccountService} from "./account.service";

@Injectable({
  providedIn: 'root'
})
export class PageService {

  private readonly accountService = inject(AccountService);
  private readonly httpClient = inject( HttpClient);

  public static readonly DEFAULT_PAGE_SORT = 9999;
  baseUrl = environment.apiUrl + "pages/";

  private _pages = signal<Page[]>([]);
  public readonly pages = this._pages.asReadonly();

  private metadataCache: { [key: number]: DownloadMetadata } = {};

  constructor() {
    effect(() => {
      const user = this.accountService.currentUser();
      if (!user) return;

      this.refreshPages().subscribe();
    });
  }

  refreshPages() {
    return this.httpClient.get<Page[]>(this.baseUrl).pipe(
      tap(pages => {
        this._pages.set(pages);
    }));
  }

  getPage(id: string): Observable<Page> {
    const page = this.pages().find(p => p.id === id);
    if (page) {
      return of(page);
    }

    return this.httpClient.get<Page>(this.baseUrl + id)
  }

  removePage(pageId: string) {
    return this.httpClient.delete(this.baseUrl + pageId).pipe(
      tap(() => {
        this._pages.update(x => x.filter(p => p.id !== pageId))
      })
    );
  }

  new(page: Page) {
    return this.httpClient.post<Page>(this.baseUrl + 'new', page).pipe(
      tap(page => {
        this._pages.update(x => {
          x.push(page);
          x.sort((a, b) => a.sortValue - b.sortValue)
          return x;
        });
      })
    );
  }

  update(page: Page) {
    return this.httpClient.post<Page>(this.baseUrl + 'update', page).pipe(
      tap(page => {
        this._pages.update(x => x.map(p => {
          if (p.id === page.id) return page;

          return p;
        }));
      })
    );
  }

  orderPages(order: string[]) {
    return this.httpClient.post(this.baseUrl + 'order', order);
  }

  loadDefault() {
    if (this.pages().length !== 0) {
      throw "Cannot load default while pages are available"
    }

    return this.httpClient.post(this.baseUrl + "load-default", {})
  }

  metadata(provider: Provider) {
    const metadata = this.metadataCache[provider];
    if (metadata) {
      return of(metadata);
    }

    return this.httpClient.get<DownloadMetadata>(this.baseUrl + `download-metadata?provider=${provider}`)
      .pipe(tap(metadata => {
        this.metadataCache[provider] = metadata;
      }))
  }

}
