import {ChangeDetectionStrategy, Component, computed, inject, model, OnInit, signal} from '@angular/core';
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {SeriesService} from "./series.service";
import {Provider} from "../../../_models/page";
import {Series} from "./_types";
import {finalize, tap} from "rxjs";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {LoadingSpinnerComponent} from "../../../shared/_component/loading-spinner/loading-spinner.component";
import {UtcToLocalTimePipe} from "../../../_pipes/utc-to-local.pipe";

@Component({
  selector: 'app-series-info',
  imports: [
    TranslocoDirective,
    LoadingSpinnerComponent,
    UtcToLocalTimePipe
  ],
  templateUrl: './series-info.component.html',
  styleUrl: './series-info.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeriesInfoComponent implements OnInit {

  private readonly modal = inject(NgbActiveModal);
  private readonly seriesService = inject(SeriesService);

  provider = model.required<Provider>();
  seriesId = model.required<string>();

  protected loading = signal(true);
  protected series = signal<Series | null>(null);

  protected seriesTitle = computed(() => this.series()?.title ?? translate('shared.unknown'));
  protected coverUrl = computed(() => {
    const url = this.series()?.coverUrl;
    if (!url) return null;

    if (url.startsWith('proxy')) {
      return '/api/' + url;
    }

    return url;
  });

  ngOnInit() {
    this.seriesService.getSeriesInfo(this.provider(), this.seriesId()).pipe(
      tap(series => this.series.set(series)),
      finalize(() => this.loading.set(false)),
    ).subscribe();
  }

  close() {
    this.modal.dismiss();
  }



}
