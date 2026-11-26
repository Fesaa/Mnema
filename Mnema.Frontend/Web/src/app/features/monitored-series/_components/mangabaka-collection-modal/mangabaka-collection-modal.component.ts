import {ChangeDetectionStrategy, Component, inject, model, OnInit, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {TranslocoDirective} from '@jsverse/transloco';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {MonitoredSeries, MonitoredSeriesService} from '@mnema/features/monitored-series/monitored-series.service';
import {MangabakaCollection, MetadataService} from '@mnema/features/monitored-series/metadata.service';
import {ToastService} from '@mnema/_services/toast.service';
import {catchError, of, switchMap, tap} from 'rxjs';

@Component({
  selector: 'app-mangabaka-collection-modal',
  imports: [
    FormsModule,
    TranslocoDirective
  ],
  templateUrl: './mangabaka-collection-modal.component.html',
  styleUrl: './mangabaka-collection-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MangabakaCollectionModalComponent implements OnInit {

  private readonly modal = inject(NgbActiveModal);
  private readonly metadataService = inject(MetadataService);
  private readonly monitoredSeriesService = inject(MonitoredSeriesService);
  private readonly toastService = inject(ToastService);

  series = model.required<MonitoredSeries>();

  collections = signal<MangabakaCollection[]>([]);
  loading = signal(true);
  saving = signal(false);
  selectedCollectionId = signal('');

  ngOnInit(): void {
    const series = this.series();
    this.selectedCollectionId.set(series.mangaBakaCollectionId ?? '');

    this.metadataService.getCollections(series.mangaBakaId).pipe(
      tap(collections => {
        this.collections.set(collections);

        if (!collections.some(c => c.id === this.selectedCollectionId())) {
          this.selectedCollectionId.set('');
        }
      }),
      catchError(() => of([] as MangabakaCollection[])),
      tap(() => this.loading.set(false)),
    ).subscribe();
  }

  collectionLabel(collection: MangabakaCollection): string {
    const parts = [
      collection.title ?? collection.id,
      collection.publisherName,
      [collection.type, collection.medium, collection.languageIso].filter(p => !!p).join(' / '),
      `${collection.countMain + collection.countExtra + collection.countOther}`,
    ];

    return parts.filter(p => !!p).join(' — ');
  }

  close() {
    this.modal.dismiss();
  }

  save() {
    const series = {...this.series(), mangaBakaCollectionId: this.selectedCollectionId()};

    this.saving.set(true);

    this.monitoredSeriesService.update(series).pipe(
      tap(() => this.toastService.successLoco('mangabaka-collection-modal.toasts.success', {name: series.title})),
      switchMap(() => this.monitoredSeriesService.refreshMetadata(series.id)),
      tap(() => this.modal.close(series)),
      catchError(err => {
        this.toastService.errorLoco('mangabaka-collection-modal.toasts.error', {name: series.title}, {msg: err.error?.message ?? ''});
        return of(null);
      }),
      tap(() => this.saving.set(false)),
    ).subscribe();
  }

}
