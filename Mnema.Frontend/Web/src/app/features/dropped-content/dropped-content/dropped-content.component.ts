import {Component, EventEmitter, inject, OnInit, signal} from '@angular/core';
import {
  FileDragAndDropUploadComponent
} from "@mnema/shared/_component/file-drag-and-drop-upload/file-drag-and-drop-upload.component";
import {NgxFileDropEntry} from "ngx-file-drop";
import {TypeaheadComponent, TypeaheadSettings} from "@mnema/type-ahead/typeahead.component";
import {MonitoredSeries, MonitoredSeriesService} from "@mnema/features/monitored-series/monitored-series.service";
import {catchError, map, of, tap} from "rxjs";
import {TranslocoDirective} from "@jsverse/transloco";
import {HttpEventType} from "@angular/common/http";
import {ActivatedRoute} from "@angular/router";

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
export class DroppedContentComponent implements OnInit {

  private readonly monitoredSeriesService = inject(MonitoredSeriesService);
  private readonly route = inject(ActivatedRoute);

  extensions = ['epub', 'cbz'].join(',');

  typeaheadConfig = signal<TypeaheadSettings<MonitoredSeries> |null>(null);

  files = signal<NgxFileDropEntry[]>([]);
  monitoredSeriesId = signal('');
  uploading = signal(false);
  progress = signal(0);

  resetTrackedFiles = new EventEmitter();

  ngOnInit() {
    const monitoredSeriesId = this.route.snapshot.queryParamMap.get('monitoredSeriesId');
    if (monitoredSeriesId !== null) {
      this.monitoredSeriesId.set(monitoredSeriesId);
    }
    this.createTypeaheadConfig();
  }

  private createTypeaheadConfig() {
    const typeaheadConfig = new TypeaheadSettings<MonitoredSeries>();

    typeaheadConfig.id = 'monitored-series-typeahead';
    typeaheadConfig.fetchFn = (f) => this.monitoredSeriesService.all(f, null, 0, 20).pipe(
      map(pl => pl.items)
    );
    typeaheadConfig.minCharacters = 2;
    typeaheadConfig.trackByIdentityFn = (_, item) => item.id;
    typeaheadConfig.getDisplayText = ms => ms.title;

    if (!this.monitoredSeriesId()) {
      this.typeaheadConfig.set(typeaheadConfig);
      return;
    }

    this.monitoredSeriesService.get(this.monitoredSeriesId()).pipe(
      catchError(() => of(null)),
      tap(ms => {
        if (ms === null) {
          this.monitoredSeriesId.set('');
        } else {
          typeaheadConfig.savedData = ms;
        }

        this.typeaheadConfig.set(typeaheadConfig);
      })
    ).subscribe();
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
