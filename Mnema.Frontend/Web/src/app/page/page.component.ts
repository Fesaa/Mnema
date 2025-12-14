import {Component, computed, effect, inject, OnInit, signal} from '@angular/core';
import {NavService} from "../_services/nav.service";
import {PageService} from "../_services/page.service";
import {DownloadMetadata, Page, Provider} from "../_models/page";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import {SearchRequest} from "../_models/search";
import {SearchInfo} from "../_models/Info";
import {SearchResultComponent} from "./_components/search-result/search-result.component";
import {SubscriptionService} from "../_services/subscription.service";
import {ProviderNamePipe} from "../_pipes/provider-name.pipe";
import {ToastService} from "../_services/toast.service";
import {ContentService} from "../_services/content.service";
import {TranslocoDirective} from "@jsverse/transloco";
import {PageLoader, PaginatorComponent} from "../shared/_component/paginator/paginator.component";
import {SearchFormComponent} from "./_components/search-form/search-form.component";
import {fadeOut} from "../_animations/fade-out";
import {LoadingSpinnerComponent} from "../shared/_component/loading-spinner/loading-spinner.component";
import {EMPTY_PAGE, PagedList} from "../_models/paged-list";

@Component({
  selector: 'app-page',
  imports: [
    ReactiveFormsModule,
    SearchResultComponent,
    FormsModule,
    TranslocoDirective,
    PaginatorComponent,
    SearchFormComponent,
    LoadingSpinnerComponent,
  ],
  templateUrl: './page.component.html',
  styleUrl: './page.component.scss',
  animations: [fadeOut]
})
export class PageComponent implements OnInit {

  private readonly navService = inject(NavService);
  private readonly pageService = inject(PageService);
  private readonly contentService = inject(ContentService);
  private readonly toastService = inject(ToastService);
  private readonly subscriptionService = inject(SubscriptionService);
  private readonly providerNamePipe = inject(ProviderNamePipe);

  page = signal<Page | undefined>(undefined);
  providers = signal<Provider[]>([]);
  metadata = signal<Map<Provider, DownloadMetadata>>(new Map());

  loading = signal(false);
  showForm = signal(true);

  searchRequest = signal<SearchRequest | null>(null);
  pageLoader = computed<PageLoader<SearchInfo> | null>(() => {
    const req = this.searchRequest();
    if (!req) return null;

    console.log("new search request")
    return (pn, pz) => this.contentService.search(req, pn, pz);
  });


  constructor() {
    effect(() => {
      const page = this.page();
      if (!page) return;

      this.loadMetadata(page);
    });
  }

  ngOnInit(): void {
    this.navService.pageIndex$.subscribe(index => {
      if (!index) return;

      this.pageService.getPage(index).subscribe(page => {
        this.page.set(page);
        this.showForm.set(true);
      });
    })

    this.subscriptionService.providers().subscribe(providers => {
      this.providers.set(providers);
    })
  }

  getDownloadMetadata(provider: Provider) {
    return this.metadata().get(provider) ?? {definitions: []}
  }

  search(req: SearchRequest) {
    if (this.loading()) {
      return;
    }

    this.showForm.set(false);

    req.provider = (this.page()?.providers[0] ?? 0) as any as number[];

    this.searchRequest.set(req);
  }

  private loadMetadata(page: Page) {
    for (const provider of page.providers) {
      this.pageService.metadata(provider).subscribe({
        next: metadata => {
          this.metadata.update(m => {
            m.set(provider, metadata);
            return m;
          })
        },
        error: error => {
          this.toastService.errorLoco("page.toasts.metadata-failed",
            {provider: this.providerNamePipe.transform(provider)}, {msg: error.error.message});
        }
      })
    }
  }
}
