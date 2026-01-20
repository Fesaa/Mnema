import {Component, computed, EventEmitter, inject, OnInit, signal} from '@angular/core';
import {ModalService} from "@mnema/_services/modal.service";
import {NavService} from "@mnema/_services/nav.service";
import {MonitoredSeries, MonitoredSeriesService} from "../monitored-series.service";
import {ToastService} from "@mnema/_services/toast.service";
import {PageService} from "@mnema/_services/page.service";
import {dropAnimation} from "@mnema/_animations/drop-animation";
import {FormControlDefinition} from "@mnema/generic-form/form";
import {FormControl, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {ProviderNamePipe} from "@mnema/_pipes/provider-name.pipe";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {TableComponent} from "@mnema/shared/_component/table/table.component";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {takeUntilDestroyed, toSignal} from "@angular/core/rxjs-interop";
import {debounceTime, distinctUntilChanged, tap} from "rxjs";
import {
  EditMonitoredSeriesModalComponent
} from "@mnema/features/monitored-series/_components/edit-monitored-series-modal/edit-monitored-series-modal.component";
import {DefaultModalOptions} from "@mnema/_models/default-modal-options";
import {Provider} from "@mnema/_models/page";
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

  add() {
    this.edit({
      chapters: [],
      lastDataRefreshUtc: '',
      summary: "",
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
    component.metadata.set(this.metadata());

    this.modalService.onClose$(modal, false).pipe(
      tap(() => this.pageReloader.emit())
    ).subscribe();
  }
}
