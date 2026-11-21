import {ChangeDetectionStrategy, Component, computed, DestroyRef, EventEmitter, inject, OnInit, signal} from '@angular/core';
import {PageLoader, PaginatorComponent} from "@mnema/shared/_component/paginator/paginator.component";
import {FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators} from "@angular/forms";
import {takeUntilDestroyed, toSignal} from "@angular/core/rxjs-interop";
import {ActivatedRoute} from "@angular/router";
import {debounceTime, distinctUntilChanged, map, of, tap} from "rxjs";
import {EMPTY_PAGE} from "@mnema/_models/paged-list";
import {DefaultModalOptions} from "@mnema/_models/default-modal-options";
import {Provider} from "@mnema/_models/page";
import {FormControlDefinition} from "@mnema/generic-form/form";
import {ModalService} from "@mnema/_services/modal.service";
import {PageService} from "@mnema/_services/page.service";
import {TranslocoDirective} from "@jsverse/transloco";
import {
  CompactSeriesInfoComponent
} from "@mnema/features/monitored-series/_components/compact-series-info/compact-series-info.component";
import {
  MetadataProvider,
  MetadataSearchResult,
  MetadataService
} from "@mnema/features/monitored-series/metadata.service";
import {
  EditMonitoredSeriesModalComponent
} from "@mnema/features/monitored-series/_components/edit-monitored-series-modal/edit-monitored-series-modal.component";
import {Series} from "@mnema/page/_components/series-info/_types";

@Component({
  selector: 'app-series-search',
  imports: [
    PaginatorComponent,
    FormsModule,
    ReactiveFormsModule,
    CompactSeriesInfoComponent,
    TranslocoDirective
  ],
  templateUrl: './series-search.component.html',
  styleUrl: './series-search.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeriesSearchComponent implements OnInit {

  private readonly metadataService = inject(MetadataService);
  private readonly modalService = inject(ModalService);
  private readonly pageService = inject(PageService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  metadata = signal<Map<Provider, FormControlDefinition[]>>(new Map());

  searchForm = new FormGroup({
    query: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
    provider: new FormControl(MetadataProvider.Hardcover, {nonNullable: true, validators: [Validators.required]}),
  });

  searchOptions = toSignal(this.searchForm.valueChanges.pipe(
    distinctUntilChanged(),
    debounceTime(200),
    map(v => ({
      query: v.query ?? '',
      provider: v.provider ?? MetadataProvider.Hardcover,
    })),
  ), { initialValue: { query: '', provider: MetadataProvider.Hardcover } });

  pageLoader = computed<PageLoader<MetadataSearchResult>>(() => {
    const searchOptions = this.searchOptions();

    if (!searchOptions.query) {
      return () => of(EMPTY_PAGE);
    }

    return (pn, pz) => this.metadataService.search(
      searchOptions.provider,
      searchOptions.query,
      pn,
      pz
    );
  });

  pageReloader = new EventEmitter<void>();

  constructor() {
    this.route.queryParamMap.pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(params => {
      const query = params.get('query');
      if (query === null) return;

      this.searchForm.patchValue({
        query,
        provider: Number(params.get('provider')) === MetadataProvider.Mangabaka
          ? MetadataProvider.Mangabaka
          : MetadataProvider.Hardcover,
      });
    });
  }

  ngOnInit(){
    this.pageService.monitoredSeriesMetadata().pipe(
      tap(m => this.metadata.set(m))
    ).subscribe();
  }

  monitor(series: Series) {
    const validTitles = [series.title];
    if (series.localizedSeries && !validTitles.includes(series.localizedSeries)) {
      validTitles.push(series.localizedSeries);
    }

    const [modal, component] = this.modalService.open(EditMonitoredSeriesModalComponent, DefaultModalOptions);
    component.series.set({
      id: '',
      title: series.title,
      validTitles: validTitles,
      provider: Provider.NYAA,
      baseDir: '',
      contentFormat: 0,
      format: 0,
      titleOverride: '',
      hardcoverId: this.searchOptions().provider === MetadataProvider.Hardcover ? series.id : '',
      mangaBakaId: this.searchOptions().provider === MetadataProvider.Mangabaka ? series.id : '',
      externalId: '',
      summary: "",
      lastDataRefreshUtc: '',
      chapters: [],
      unMatchedChapters: [],
      metadata: {},
    });

    this.modalService.onClose$(modal).pipe(
      tap(() => this.pageReloader.emit()),
    ).subscribe();
  }


  protected readonly MetadataProvider = MetadataProvider;
}
