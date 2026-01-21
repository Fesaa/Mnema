import {ChangeDetectionStrategy, Component, inject, linkedSignal} from '@angular/core';
import {ActivatedRoute, Router} from "@angular/router";
import {toSignal} from "@angular/core/rxjs-interop";
import {
  MonitoredChapterStatus,
  MonitoredChapterStatuses,
  MonitoredSeries,
  MonitoredSeriesService
} from "@mnema/features/monitored-series/monitored-series.service";
import {CommonModule} from "@angular/common";
import {MonitoredChapterStatusPipe} from "@mnema/features/monitored-series/pipes/monitored-chapter-status.pipe";
import {ProviderNamePipe} from "@mnema/_pipes/provider-name.pipe";
import {TagBadgeComponent} from "@mnema/shared/_component/tag-badge/tag-badge.component";
import {ContentFormatPipe} from "@mnema/features/monitored-series/pipes/content-format.pipe";
import {FormatPipe} from "@mnema/features/monitored-series/pipes/format.pipe";
import {TranslocoDirective, TranslocoService} from "@jsverse/transloco";
import {UtcToLocalTimePipe} from "@mnema/_pipes/utc-to-local.pipe";
import {BadgeComponent} from "@mnema/shared/_component/badge/badge.component";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {ModalService} from "@mnema/_services/modal.service";
import {filter, map, switchMap, tap} from "rxjs";
import {SeriesInfoComponent} from "@mnema/page/_components/series-info/series-info.component";
import {DefaultModalOptions} from "@mnema/_models/default-modal-options";
import {
  EditMonitoredSeriesModalComponent
} from "@mnema/features/monitored-series/_components/edit-monitored-series-modal/edit-monitored-series-modal.component";
import {ListSelectModalComponent} from "@mnema/shared/_component/list-select-modal/list-select-modal.component";
import {EventType, SignalRService} from "@mnema/_services/signal-r.service";
import {SearchInfo} from "@mnema/_models/Info";
import {DownloadModalComponent} from "@mnema/page/_components/download-modal/download-modal.component";
import {ToastService} from "@mnema/_services/toast.service";

@Component({
  selector: 'app-monitored-series',
  standalone: true,
  imports: [CommonModule, MonitoredChapterStatusPipe, ProviderNamePipe, TagBadgeComponent, ContentFormatPipe, FormatPipe, TranslocoDirective, UtcToLocalTimePipe, BadgeComponent, NgbTooltip],
  templateUrl: './monitored-series.component.html',
  styleUrl: './monitored-series.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MonitoredSeriesComponent {

  private readonly chapterStatusPipe = new MonitoredChapterStatusPipe();

  private readonly monitoredSeriesService = inject(MonitoredSeriesService);
  private readonly transLoco = inject(TranslocoService);
  private readonly modalService = inject(ModalService);
  private readonly signalR = inject(SignalRService);
  private readonly route = inject(ActivatedRoute);
  private readonly toastR = inject(ToastService);
  private readonly router = inject(Router);
  private readonly data = toSignal(this.route.data);

  protected series = linkedSignal(() => this.data()!['series'] as MonitoredSeries);

  constructor() {
    this.signalR.events$.pipe(
      filter(e => e.type === EventType.MetadataRefreshed),
      map(e => e.data.seriesId as string),
      filter(id => id === this.series().id),
      switchMap(() => this.monitoredSeriesService.get(this.series().id)),
      tap(series => this.series.set(series))
    ).subscribe();
  }

  setChapterStatus(chapterId: string) {
    const [modal, component] = this.modalService.open(ListSelectModalComponent, {
      size: "lg", centered: true
    });

    component.title.set(this.transLoco.translate('monitored-series-detail.select-status'));
    component.inputItems.set(MonitoredChapterStatuses.map(s =>
      ({value: s, label: this.chapterStatusPipe.transform(s)})));
    component.requireConfirmation.set(true);

    this.modalService.onClose$<MonitoredChapterStatus>(modal).pipe(
      switchMap(status => this.monitoredSeriesService.setChapterStatus(this.series().id, chapterId, status)),
      switchMap(() => this.monitoredSeriesService.get(this.series().id)),
      tap(series => this.series.set(series)),
    ).subscribe();
  }

  refreshMetadata() {
    this.modalService.confirm$({
      question: this.transLoco.translate('monitored-series-detail.confirm-refresh-metadata', {name: this.series().title})
    }, true).pipe(
      switchMap(() => this.monitoredSeriesService.refreshMetadata(this.series().id))
    ).subscribe();
  }

  search() {
    this.monitoredSeriesService.search(this.series().id).pipe(
      switchMap(results => {
        const [modal, component] = this.modalService.open(ListSelectModalComponent, {
          size: "lg", centered: true
        });
        component.title.set(this.transLoco.translate('monitored-series-detail.select-search-result'));
        component.inputItems.set(results.items.map(si => ({label: si.name, value: si})));
        component.itemsBeforeVirtual.set(8);
        component.requireConfirmation.set(true);

        return this.modalService.onClose$<SearchInfo>(modal)
      }),
      switchMap(selection => this.monitoredSeriesService.download(this.series().id, selection)),
      tap(() => this.toastR.infoLoco('monitored-series-detail.download-started', {name: this.series().title}))
    ).subscribe();
  }

  showResolvedSeries() {
    this.monitoredSeriesService.resolvedSeries(this.series().id).pipe(
      tap(series => {
        const [_, component] = this.modalService.open(SeriesInfoComponent, DefaultModalOptions)
        component.series.set(series);
      })
    ).subscribe();
  }

  edit() {
    const [_, component] = this.modalService.open(EditMonitoredSeriesModalComponent, DefaultModalOptions);
    component.series.set(this.series());
  }

  delete() {
    this.modalService.confirm$({
      question: this.transLoco.translate('monitored-series-detail.confirm-delete', {name: this.series().title})
    }, true).pipe(
      switchMap(() => this.monitoredSeriesService.delete(this.series().id)),
      tap(() => this.router.navigateByUrl('home')),
    ).subscribe();
  }

  getStatusClass(status: MonitoredChapterStatus): string {
    switch (status) {
      case MonitoredChapterStatus.NotMonitored:
        return 'status-not-monitored';
      case MonitoredChapterStatus.Missing:
        return 'status-missing';
      case MonitoredChapterStatus.Upcoming:
        return 'status-upcoming';
      case MonitoredChapterStatus.Importing:
        return 'status-importing';
      case MonitoredChapterStatus.Available:
        return 'status-available';
      default:
        return '';
    }
  }
}
