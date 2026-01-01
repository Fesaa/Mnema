import {Component, computed, inject, OnInit, signal} from '@angular/core';
import {NavService} from "../_services/nav.service";
import {PageService} from "../_services/page.service";
import {Page, Provider} from "../_models/page";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import {SearchRequest} from "../_models/search";
import {SearchInfo} from "../_models/Info";
import {SearchResultComponent} from "./_components/search-result/search-result.component";
import {SubscriptionService} from "../_services/subscription.service";
import {ContentService} from "../_services/content.service";
import {TranslocoDirective} from "@jsverse/transloco";
import {PageLoader, PaginatorComponent} from "../shared/_component/paginator/paginator.component";
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
  private readonly subscriptionService = inject(SubscriptionService);

  page = signal<Page | undefined>(undefined);

  providers = signal<Provider[]>([]);

  metadata = computed(() => this.page()?.metadata);

  loading = signal(false);
  showForm = signal(true);

  searchRequest = signal<SearchRequest | null>(null);
  pageLoader = computed<PageLoader<SearchInfo> | null>(() => {
    const req = this.searchRequest();
    if (!req) return null;

    return (pn, pz) => this.contentService.search(req, pn, pz);
  });

  ngOnInit(): void {
    this.navService.pageId$.subscribe(index => {
      if (!index) return;

      this.pageService.getPage(index).subscribe(page => {
        this.page.set(page);
        this.searchRequest.set(null);
        this.showForm.set(true);
      });
    })

    this.subscriptionService.providers().subscribe(providers => {
      this.providers.set(providers);
    })
  }

  search(req: SearchRequest) {
    if (this.loading()) {
      return;
    }

    this.showForm.set(false);

    req.provider = this.page()!.provider;

    this.searchRequest.set(req);
  }
}
