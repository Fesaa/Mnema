import {Component, computed, EventEmitter, inject, OnInit, signal} from '@angular/core';
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
import {RouterLink} from "@angular/router";
import {PaginatorComponent} from "@mnema/shared/_component/paginator/paginator.component";
import {TranslocoDirective} from "@jsverse/transloco";
import {UtcToLocalTimePipe} from "@mnema/_pipes/utc-to-local.pipe";

@Component({
  selector: 'app-monitored-series-manager',
  imports: [
    TranslocoDirective,
    ReactiveFormsModule,
    RouterLink,
    PaginatorComponent,
    UtcToLocalTimePipe,
  ],
  templateUrl: './monitored-series-manager.component.html',
  styleUrl: './monitored-series-manager.component.scss',
  animations: [dropAnimation]
})
export class MonitoredSeriesManagerComponent implements OnInit {

  private readonly modalService = inject(ModalService);
  private readonly navService = inject(NavService);
  protected readonly monitoredSeriesService = inject(MonitoredSeriesService);
  private readonly toastService = inject(ToastService);
  private readonly pageService = inject(PageService);

  metadata = signal<Map<Provider, FormControlDefinition[]>>(new Map());
  hasAny = signal(false);

  pageLoader = computed(() => {
    const filter = this.filter();

    return (pn: number, ps: number) => {
      return this.monitoredSeriesService.all(filter.filterText ?? '', pn, ps);
    }
  });

  filterForm = new FormGroup({
    filterText: new FormControl(''),
  });
  filter = toSignal(this.filterForm.valueChanges.pipe(
    debounceTime(400),
    takeUntilDestroyed(),
    distinctUntilChanged(),
  ), { initialValue: { filterText: '' } });

  pageReloader = new EventEmitter<void>();

  ngOnInit(): void {
    this.navService.setNavVisibility(true);

    this.pageService.monitoredSeriesMetadata().pipe(
      tap(m => this.metadata.set(m))
    ).subscribe();
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
