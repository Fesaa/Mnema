import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {ActivatedRoute, Router} from "@angular/router";
import {toSignal} from "@angular/core/rxjs-interop";
import {
  MonitoredSeries,
  MonitoredChapterStatus,
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
import {switchMap, tap} from "rxjs";
import {SeriesInfoComponent} from "@mnema/page/_components/series-info/series-info.component";
import {DefaultModalOptions} from "@mnema/_models/default-modal-options";
import {
  EditMonitoredSeriesModalComponent
} from "@mnema/features/monitored-series/_components/edit-monitored-series-modal/edit-monitored-series-modal.component";

@Component({
  selector: 'app-monitored-series',
  standalone: true,
  imports: [CommonModule, MonitoredChapterStatusPipe, ProviderNamePipe, TagBadgeComponent, ContentFormatPipe, FormatPipe, TranslocoDirective, UtcToLocalTimePipe, BadgeComponent, NgbTooltip],
  templateUrl: './monitored-series.component.html',
  styleUrl: './monitored-series.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MonitoredSeriesComponent {

  private readonly monitoredSeriesService = inject(MonitoredSeriesService);
  private readonly router = inject(Router);
  private readonly modalService = inject(ModalService);
  private readonly transLoco = inject(TranslocoService);
  private readonly route = inject(ActivatedRoute);
  private readonly data = toSignal(this.route.data);

  protected series = computed(() => this.data()!['series'] as MonitoredSeries);

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
