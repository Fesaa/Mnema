import {Component, computed, effect, EventEmitter, inject, OnInit, signal} from '@angular/core';
import {ModalService} from "@mnema/_services/modal.service";
import {NavService} from "@mnema/_services/nav.service";
import {MonitoredChapterStatus, MonitoredSeries, MonitoredSeriesService} from "../monitored-series.service";
import {ToastService} from "@mnema/_services/toast.service";
import {PageService} from "@mnema/_services/page.service";
import {dropAnimation} from "@mnema/_animations/drop-animation";
import {FormControlDefinition} from "@mnema/generic-form/form";
import {FormControl, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {takeUntilDestroyed, toSignal} from "@angular/core/rxjs-interop";
import {debounceTime, distinctUntilChanged, tap} from "rxjs";
import {Provider} from "@mnema/_models/page";
import {ActivatedRoute, Router, RouterLink} from "@angular/router";
import {PaginatorComponent} from "@mnema/shared/_component/paginator/paginator.component";
import {TranslocoDirective} from "@jsverse/transloco";
import {UtcToLocalTimePipe} from "@mnema/_pipes/utc-to-local.pipe";
import {ProviderNamePipe} from "@mnema/_pipes/provider-name.pipe";
import {querySignal} from "@mnema/shared/signals";

type Filter = {
  filterText: string;
  provider: Provider | null;
}

@Component({
  selector: 'app-monitored-series-manager',
  imports: [
    TranslocoDirective,
    ReactiveFormsModule,
    RouterLink,
    PaginatorComponent,
    UtcToLocalTimePipe,
    ProviderNamePipe,
  ],
  templateUrl: './monitored-series-manager.component.html',
  styleUrl: './monitored-series-manager.component.scss',
  animations: [dropAnimation]
})
export class MonitoredSeriesManagerComponent implements OnInit {

  private readonly navService = inject(NavService);
  protected readonly monitoredSeriesService = inject(MonitoredSeriesService);
  private readonly pageService = inject(PageService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  hasAny = signal(false);
  providers = signal<Provider[]>([]);

  filterQuery = querySignal<Filter>({
    filterText: '',
    provider: null,
  }, this.route, this.router);

  filterForm = new FormGroup({
    filterText: new FormControl(''),
    provider: new FormControl<Provider | null>(null),
  });

  pageLoader = computed(() => {
    const query = this.filterQuery();
    return (pn: number, ps: number) =>
      this.monitoredSeriesService.all(query.filterText, query.provider, pn, ps);
  });
  pageReloader = new EventEmitter<void>();

  constructor() {
    this.filterForm.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      takeUntilDestroyed()
    ).subscribe(val => {
      this.filterQuery.set({
        filterText: val.filterText ?? '',
        provider: val.provider ?? null
      });
    });

    effect(() => {
      const query = this.filterQuery();
      this.filterForm.patchValue({
        filterText: query.filterText,
        provider: query.provider ? parseInt(query.provider + '') : null,
      }, { emitEvent: false });
    });
  }

  ngOnInit(): void {
    this.navService.setNavVisibility(true);
    this.pageService.allowedProviders().subscribe(p => this.providers.set(p));
  }

  nextReleaseDate(series: MonitoredSeries) {
    const upcomingWithReleaseDate = series.chapters
      .filter(c => c.status === MonitoredChapterStatus.Upcoming)
      .filter(c => c.releaseDate !== null);

    if (upcomingWithReleaseDate.length > 0) {
      return upcomingWithReleaseDate[0].releaseDate;
    }

    return null;
  }

}
