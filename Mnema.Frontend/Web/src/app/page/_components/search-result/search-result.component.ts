import {Component, inject, input, OnInit, signal} from '@angular/core';
import {SearchInfo} from "@mnema/_models/Info";
import {Page} from "@mnema/_models/page";
import {bounceIn200ms} from "@mnema/_animations/bounce-in";
import {dropAnimation} from "@mnema/_animations/drop-animation";
import {ImageService} from "@mnema/_services/image.service";
import {TranslocoDirective} from "@jsverse/transloco";
import {NgStyle} from "@angular/common";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {ModalService} from "@mnema/_services/modal.service";
import {DownloadModalComponent} from "../download-modal/download-modal.component";
import {DefaultModalOptions} from "@mnema/_models/default-modal-options";
import {SeriesInfoComponent} from "../series-info/series-info.component";
import {FormControlDefinition} from "@mnema/generic-form/form";
import {SeriesService} from "@mnema/page/_components/series-info/series.service";
import {tap} from "rxjs";
import {
  EditMonitoredSeriesModalComponent
} from "@mnema/features/monitored-series/_components/edit-monitored-series-modal/edit-monitored-series-modal.component";
import {ContentFormat, Format, MonitoredSeries} from "@mnema/features/monitored-series/monitored-series.service";

@Component({
  selector: 'app-search-result',
  imports: [
    TranslocoDirective,
    NgStyle,
    NgbTooltip,
  ],
  templateUrl: './search-result.component.html',
  styleUrl: './search-result.component.scss',
  animations: [bounceIn200ms, dropAnimation]
})
export class SearchResultComponent implements OnInit{

  private readonly imageService = inject(ImageService);
  private readonly modalService = inject(ModalService);
  private readonly seriesService = inject(SeriesService);

  page = input.required<Page>();
  searchResult = input.required<SearchInfo>();
  metadata = input.required<FormControlDefinition[]>();

  imageSource = signal<string | null>(null);


  ngOnInit(): void {
    this.loadImage();
  }

  monitorSeries() {
    const [_, component] = this.modalService.open(EditMonitoredSeriesModalComponent, DefaultModalOptions);

    const newMonitoredSeries: MonitoredSeries = {
      externalId: this.searchResult().id,
      baseDir: "",
      chapters: [],
      contentFormat: ContentFormat.Manga,
      format: Format.Archive,
      hardcoverId: "",
      lastDataRefreshUtc: "",
      mangabakaId: "",
      provider: this.searchResult().provider,
      summary: "",
      titleOverride: "",
      validTitles: [],
      id: '',
      title: this.searchResult().name

    }

    component.series.set(newMonitoredSeries);
  }

  download() {
    const metadata = this.metadata();
    if (!metadata) return

    const page = this.page();
    const defaultDir = page.customRootDir;

    const [_, component] = this.modalService.open(DownloadModalComponent, DefaultModalOptions);
    component.metadata.set(metadata);
    component.defaultDir.set(defaultDir);
    component.rootDir.set(page.customRootDir);
    component.info.set(this.searchResult());
  }

  loadInfo() {
    this.seriesService.getSeriesInfo(this.searchResult().provider, this.searchResult().id).pipe(
      tap(series => {
        const [_, component] = this.modalService.open(SeriesInfoComponent, DefaultModalOptions);
        component.series.set(series);
      }),
    ).subscribe();
  }

  loadImage() {
    if (this.searchResult().imageUrl === "") {
      return;
    }

    if (this.searchResult().imageUrl.startsWith("proxy")) {
      this.imageService.getImage(this.searchResult().imageUrl).subscribe(src => {
        this.imageSource.set(src);
      })
    } else {
      this.imageSource.set(this.searchResult().imageUrl);
    }
  }

}
