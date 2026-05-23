import {ChangeDetectionStrategy, Component, computed, inject, input, output} from '@angular/core';
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {Series} from "@mnema/page/_components/series-info/_types";
import {MetadataSearchResult} from "@mnema/features/monitored-series/metadata.service";
import {Router} from "@angular/router";

@Component({
  selector: 'app-compact-series-info',
  imports: [TranslocoDirective],
  templateUrl: './compact-series-info.component.html',
  styleUrl: './compact-series-info.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CompactSeriesInfoComponent {

  private readonly router = inject(Router);

  series = input.required<Series>();
  buttonLabel = input<string>();
  buttonIcon = input<string>();

  buttonClick = output<Series>();

  protected seriesTitle = computed(() => this.series()?.title ?? translate('shared.unknown'));

  protected monitoredSeriesId = computed(() => {
    const series = this.series();
    if (Object.hasOwn(series, 'monitoredSeriesId')) {
      return (series as MetadataSearchResult).monitoredSeriesId;
    }

    return [];
  });
  isMonitored = computed(() => this.monitoredSeriesId().length > 0);

  protected firstChapter = computed(() => {
    if (this.series().chapters.length > 0) {
      return this.series().chapters[0];
    }
    return null;
  });

  protected coverUrl = computed(() => {
    const url = this.series().coverUrl ?? this.firstChapter()?.coverUrl;
    if (!url) return null;

    if (url.startsWith('proxy')) {
      return '/api/' + url;
    }
    return url;
  });

  protected summary = computed(() => {
    const series = this.series();
    const seriesSummary = series.summary;
    if (seriesSummary) {
      return series.summary;
    }

    const firstChapter = this.firstChapter();
    if (firstChapter) {
      return firstChapter.summary;
    }
    return null;
  });

  protected mainLink = computed(() => this.series().refUrl);

  openMonitoredSeries() {
    const ids = this.monitoredSeriesId();
    if (ids.length === 0) return;

    if (ids.length === 1) {
      this.router.navigateByUrl('/monitored-series-detail/' +  ids[0]).catch(console.error);
      return;
    }

    // TODO: Figure out how to handle multiple series
  }

  onButtonClick() {
    this.buttonClick.emit(this.series());
  }
}
