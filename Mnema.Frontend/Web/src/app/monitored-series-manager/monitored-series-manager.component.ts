import {Component, computed, effect, EventEmitter, inject, OnInit, signal} from '@angular/core';
import {NavService} from "../_services/nav.service";
import {MonitoredSeries, MonitoredSeriesService} from '../_services/monitored-series.service';
import {dropAnimation} from "../_animations/drop-animation";
import {ToastService} from "../_services/toast.service";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {TableComponent} from "../shared/_component/table/table.component";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {ModalService} from "../_services/modal.service";
import {catchError, debounceTime, distinctUntilChanged, forkJoin, map, of, switchMap, tap} from "rxjs";
import {FormControl, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {takeUntilDestroyed, toSignal} from "@angular/core/rxjs-interop";
import {ProviderNamePipe} from "../_pipes/provider-name.pipe";
import {EditMonitoredSeriesModalComponent} from "./_components/edit-monitored-series-modal/edit-monitored-series-modal.component";
import {DefaultModalOptions} from "../_models/default-modal-options";
import {Provider} from "../_models/page";
import {FormControlDefinition} from "../generic-form/form";
import {PageService} from "../_services/page.service";
import {RouterLink} from "@angular/router";

@Component({
  selector: 'app-monitored-series-manager',
  imports: [
    TranslocoDirective,
    TableComponent,
    NgbTooltip,
    ReactiveFormsModule,
    ProviderNamePipe,
    RouterLink,
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
  allowedProviders = signal<Provider[]>([]);
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

    // Load metadata for providers. For now we only support Nyaa for monitored series
    // But we might support more later, so we use the same pattern as subscriptions
    this.allowedProviders.set([Provider.NYAA]);

    const loaders$ = this.allowedProviders().map(
      p => this.pageService.metadata(p).pipe(
        map(m => [p, m] as [Provider, FormControlDefinition[]]),
        catchError(err => of([p, []] as [Provider, FormControlDefinition[]]))
      ));

    forkJoin(loaders$).pipe(
      tap(metadata => this.metadata.set(new Map(metadata)))
    ).subscribe();
  }

  add() {
    this.edit({
      id: '',
      title: '',
      validTitles: [],
      providers: [],
      baseDir: '',
      contentFormat: 0,
      format: 0,
      metadata: {}
    });
  }

  async delete(series: MonitoredSeries) {
    if (!await this.modalService.confirm({
      question: translate("monitored-series.confirm-delete", {title: series.title})
    })) {
      return;
    }

    this.monitoredSeriesService.delete(series.id).pipe(
      tap(() => {
        this.toastService.successLoco("monitored-series.toasts.delete.success", {name: series.title});
        this.pageReloader.emit();
      })
    ).subscribe();
  }

  trackBy(idx: number, series: MonitoredSeries) {
    return series.id
  }

  edit(series: MonitoredSeries) {
    const [modal, component] = this.modalService.open(EditMonitoredSeriesModalComponent, DefaultModalOptions);
    component.series.set(series);

    // Pass metadata for the first provider since MonitoredSeries can have multiple,
    // but typically they share the same metadata requirements if they are the same type of content.
    // Subscriptions only have one provider.
    // For now, MonitoredSeries metadata is mostly used for the content itself.
    // If multiple providers are selected, we take the metadata from the first one that has it.
    const providers = series.providers.length > 0 ? series.providers : [Provider.NYAA];
    let metadata: FormControlDefinition[] = [];
    for (const p of providers) {
      const m = this.metadata().get(p);
      if (m && m.length > 0) {
        metadata = m;
        break;
      }
    }
    component.metadata.set(metadata);

    this.modalService.onClose$(modal, false).pipe(
      tap(() => this.pageReloader.emit())
    ).subscribe();
  }
}
