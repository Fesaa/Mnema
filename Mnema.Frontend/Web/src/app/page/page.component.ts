import {Component, effect, inject, OnInit, signal} from '@angular/core';
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
import {PaginatorComponent} from "../shared/_component/paginator/paginator.component";
import {SearchFormComponent} from "./_components/search-form/search-form.component";
import {fadeOut} from "../_animations/fade-out";
import {LoadingSpinnerComponent} from "../shared/_component/loading-spinner/loading-spinner.component";

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
  searchResults = signal<SearchInfo[]>([]);

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
        this.searchResults.set([]);
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

    req.provider = this.page()?.providers ?? [];
    this.loading.set(true)
    this.contentService.search(req).subscribe({
      next: info => {
        if (!info || info.length == 0) {
          this.showForm.set(true);
          this.toastService.errorLoco("page.toasts.no-results")
        } else {
          this.toastService.successLoco("page.toasts.search-success", {}, {amount: info.length});
        }
        this.searchResults.set(info ?? [])
      },
      error: error => {
        this.toastService.genericError(error.error.message);
      }
    }).add(() => this.loading.set(false));
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
