import {ChangeDetectionStrategy, Component, computed, DestroyRef, inject, model, signal} from '@angular/core';
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {TranslocoDirective} from "@jsverse/transloco";
import {catchError, EMPTY, finalize, tap} from "rxjs";
import {ParsedSeriesUrl} from "@mnema/_models/parsed-series-url";
import {DefaultModalOptions} from "@mnema/_models/default-modal-options";
import {SearchInfo} from "@mnema/_models/Info";
import {Provider} from "@mnema/_models/page";
import {ModalService} from "@mnema/_services/modal.service";
import {PageService} from "@mnema/_services/page.service";
import {ProviderNamePipe} from "@mnema/_pipes/provider-name.pipe";
import {MetadataProviderPipe} from "@mnema/_pipes/metadata-provider.pipe";
import {
  ContentFormat,
  Format,
  MonitoredSeries
} from "@mnema/features/monitored-series/monitored-series.service";
import {MetadataProvider} from "@mnema/features/monitored-series/metadata.service";
import {
  EditMonitoredSeriesModalComponent
} from "@mnema/features/monitored-series/_components/edit-monitored-series-modal/edit-monitored-series-modal.component";
import {DownloadModalComponent} from "@mnema/page/_components/download-modal/download-modal.component";

@Component({
  selector: 'app-add-from-url-result-modal',
  imports: [
    TranslocoDirective,
    ProviderNamePipe,
    MetadataProviderPipe,
  ],
  templateUrl: './add-from-url-result-modal.component.html',
  styleUrl: './add-from-url-result-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddFromUrlResultModalComponent {

  private readonly modal = inject(NgbActiveModal);
  private readonly modalService = inject(ModalService);
  private readonly pageService = inject(PageService);
  private readonly destroyRef = inject(DestroyRef);

  result = model.required<ParsedSeriesUrl>();

  loading = signal(false);

  canDownload = computed(() => this.result().provider !== null);
  canMonitor = computed(() => this.result().provider !== null || this.result().metadataProvider !== null);

  monitor() {
    const series = this.toMonitoredSeries(this.result());

    this.modal.close();

    const [_, component] = this.modalService.open(EditMonitoredSeriesModalComponent, DefaultModalOptions);
    component.series.set(series);
  }

  download() {
    const result = this.result();
    const provider = result.provider;
    if (provider === null || this.loading()) return;

    this.loading.set(true);

    this.pageService.metadata(provider).pipe(
      takeUntilDestroyed(this.destroyRef),
      tap(metadata => {
        const page = this.pageService.pages().find(p => p.provider === provider);
        const defaultDir = page?.customRootDir ?? '';

        const info: SearchInfo = {
          id: result.externalId,
          provider: provider,
          name: result.seriesName,
          description: result.description ?? '',
          downloadUrl: '',
          size: '',
          tags: [],
          imageUrl: '',
          url: '',
        };

        this.modal.close();

        const [_, component] = this.modalService.open(DownloadModalComponent, DefaultModalOptions);
        component.metadataFormDefinition.set(metadata);
        component.defaultDir.set(defaultDir);
        component.rootDir.set(defaultDir);
        component.info.set(info);
      }),
      catchError(() => EMPTY),
      finalize(() => this.loading.set(false)),
    ).subscribe();
  }

  close() {
    this.modal.dismiss();
  }

  private toMonitoredSeries(result: ParsedSeriesUrl): MonitoredSeries {
    const metadataProvider = result.metadataProvider;

    return {
      id: '',
      title: result.seriesName,
      validTitles: [result.seriesName],
      provider: metadataProvider === null ? result.provider! : Provider.NYAA,
      baseDir: '',
      contentFormat: ContentFormat.Manga,
      format: Format.Archive,
      titleOverride: '',
      hardcoverId: metadataProvider === MetadataProvider.Hardcover ? result.externalId : '',
      mangaBakaId: metadataProvider === MetadataProvider.Mangabaka ? result.externalId : '',
      externalId: metadataProvider === null ? result.externalId : '',
      summary: result.description ?? '',
      lastDataRefreshUtc: '',
      chapters: [],
      unMatchedChapters: [],
      metadata: {},
    };
  }

}
