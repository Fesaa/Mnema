import {ChangeDetectionStrategy, Component, computed, inject, OnInit, signal} from '@angular/core';
import {PageLoader, PaginatorComponent} from "@mnema/shared/_component/paginator/paginator.component";
import {FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators} from "@angular/forms";
import {toSignal} from "@angular/core/rxjs-interop";
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
  MetadataService,
  MetadataProvider,
  MetadataSearchResult
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

  ngOnInit(){
    this.pageService.monitoredSeriesMetadata().pipe(
      tap(m => this.metadata.set(m))
    ).subscribe();
  }
  monitor(series: Series) {
    const validTitles = [series.title];
    if (series.localizedSeries) {
      validTitles.push(series.title);
    }

    const [_, component] = this.modalService.open(EditMonitoredSeriesModalComponent, DefaultModalOptions);
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
      mangabakaId: this.searchOptions().provider === MetadataProvider.Mangabaka ? series.id : '',
      externalId: '',
      summary: "",
      lastDataRefreshUtc: '',
      chapters: []
    });
  }


  protected readonly MetadataProvider = MetadataProvider;
}
