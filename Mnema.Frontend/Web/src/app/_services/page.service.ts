import {effect, inject, Injectable, signal} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {environment} from "@env/environment";
import {AllProviders, Page, Provider} from "../_models/page";
import {catchError, from, map, mergeMap, Observable, of, switchMap, tap, toArray} from "rxjs";
import {AccountService} from "./account.service";
import {FormControlDefinition} from "../generic-form/form";

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

  private metadataCache: { [key: number]: FormControlDefinition[] } = {};

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

  allowedProviders() {
    return of(AllProviders);
  }

  monitoredSeriesMetadata() {
    return this.allowedProviders().pipe(
      mergeMap(providers => from(providers)),
      switchMap(p => this.metadata(p).pipe(
        map(m => [p, m] as [Provider, FormControlDefinition[]]),
        catchError(err => of([p, []] as [Provider, FormControlDefinition[]]))
      )),
      toArray(),
      map(pairs => new Map<Provider, FormControlDefinition[]>(pairs))
    );
  }

  metadata(provider: Provider) {
    const metadata = this.metadataCache[provider];
    if (metadata) {
      return of(metadata);
    }

    return this.httpClient.get<FormControlDefinition[]>(this.baseUrl + `download-metadata?provider=${provider}`)
      .pipe(tap(metadata => {
        this.metadataCache[provider] = metadata;
      }))
  }

}
