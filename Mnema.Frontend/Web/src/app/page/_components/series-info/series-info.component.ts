import {ChangeDetectionStrategy, Component, computed, inject, model, OnInit, signal} from '@angular/core';
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {SeriesService} from "./series.service";
import {Provider} from "../../../_models/page";
import {Series} from "./_types";
import {finalize, tap} from "rxjs";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {LoadingSpinnerComponent} from "../../../shared/_component/loading-spinner/loading-spinner.component";
import {UtcToLocalTimePipe} from "../../../_pipes/utc-to-local.pipe";
import {PersonRolePipe} from "@mnema/_pipes/person-role.pipe";
import {MonitoredChapterStatusPipe} from "@mnema/features/monitored-series/pipes/monitored-chapter-status.pipe";

@Component({
  selector: 'app-series-info',
  imports: [
    TranslocoDirective,
    LoadingSpinnerComponent,
    UtcToLocalTimePipe,
    PersonRolePipe,
    MonitoredChapterStatusPipe
  ],
  templateUrl: './series-info.component.html',
  styleUrl: './series-info.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeriesInfoComponent {

  private readonly modal = inject(NgbActiveModal);
  series = model.required<Series |null>();

  protected seriesTitle = computed(() => this.series()?.title ?? translate('shared.unknown'));
  protected coverUrl = computed(() => {
    const url = this.series()?.coverUrl;
    if (!url) return null;

    if (url.startsWith('proxy')) {
      return '/api/' + url;
    }

    return url;
  });

  close() {
    this.modal.dismiss();
  }



}
