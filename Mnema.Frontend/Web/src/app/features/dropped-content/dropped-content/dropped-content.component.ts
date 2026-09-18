import {Component, EventEmitter, inject, signal} from '@angular/core';
import {
  FileDragAndDropUploadComponent
} from "@mnema/shared/_component/file-drag-and-drop-upload/file-drag-and-drop-upload.component";
import {NgxFileDropEntry} from "ngx-file-drop";
import {TypeaheadComponent, TypeaheadSettings} from "@mnema/type-ahead/typeahead.component";
import {MonitoredSeries, MonitoredSeriesService} from "@mnema/features/monitored-series/monitored-series.service";
import {map} from "rxjs";
import {TranslocoDirective} from "@jsverse/transloco";
import {HttpEventType} from "@angular/common/http";

@Component({
  selector: 'app-dropped-content',
  imports: [
    FileDragAndDropUploadComponent,
    TypeaheadComponent,
    TranslocoDirective
  ],
  templateUrl: './dropped-content.component.html',
  styleUrl: './dropped-content.component.scss',
})
export class DroppedContentComponent {

  private readonly monitoredSeriesService = inject(MonitoredSeriesService);

  extensions = ['epub', 'cbz'].join(',');

  typeaheadConfig: TypeaheadSettings<MonitoredSeries>;

  files = signal<NgxFileDropEntry[]>([]);
  monitoredSeriesId = signal('');
  uploading = signal(false);
  progress = signal(0);

  resetTrackedFiles = new EventEmitter();

  constructor() {
    this.typeaheadConfig = new TypeaheadSettings<MonitoredSeries>();

    this.typeaheadConfig.id = 'monitored-series-typeahead';
    this.typeaheadConfig.fetchFn = (f) => this.monitoredSeriesService.all(f, null, 0, 20).pipe(
      map(pl => pl.items)
    );
    this.typeaheadConfig.minCharacters = 2;
    this.typeaheadConfig.trackByIdentityFn = (_, item) => item.id;
    this.typeaheadConfig.getDisplayText = ms => ms.title;
  }

  onFileDrop(files: NgxFileDropEntry[]) {
    this.files.set(files);
  }

  async startImport() {
    if (!this.monitoredSeriesId() || this.files().length === 0) return;

    this.uploading.set(true);
    this.progress.set(0);

    const upload$ = await this.monitoredSeriesService.upload(
      this.monitoredSeriesId(),
      this.files()
    );

    upload$.subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress && event.total) {
          const percentDone = Math.round((100 * event.loaded) / event.total);
          this.progress.set(percentDone);
        } else if (event.type === HttpEventType.Response) {
          this.uploading.set(false);
          this.progress.set(100);
          this.files.set([]);
          this.resetTrackedFiles.emit();
        }
      },
      error: () => {
        this.uploading.set(false);
        this.progress.set(0);
      }
    });
  }

}
