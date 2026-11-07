import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  linkedSignal, OnInit,
  signal,
  TemplateRef,
  viewChild
} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from "@angular/router";
import {toSignal} from "@angular/core/rxjs-interop";
import {
  Cover,
  FileMetadata, Format, MonitoredChapter,
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
import {translate, TranslocoDirective, TranslocoService} from "@jsverse/transloco";
import {UtcToLocalTimePipe} from "@mnema/_pipes/utc-to-local.pipe";
import {BadgeComponent} from "@mnema/shared/_component/badge/badge.component";
import {ModalService} from "@mnema/_services/modal.service";
import {EMPTY, filter, finalize, forkJoin, map, merge, switchMap, tap} from "rxjs";
import {SeriesInfoComponent} from "@mnema/page/_components/series-info/series-info.component";
import {DefaultModalOptions} from "@mnema/_models/default-modal-options";
import {
  EditMonitoredSeriesModalComponent
} from "@mnema/features/monitored-series/_components/edit-monitored-series-modal/edit-monitored-series-modal.component";
import {ListSelectModalComponent} from "@mnema/shared/_component/list-select-modal/list-select-modal.component";
import {EventType, SignalRService} from "@mnema/_services/signal-r.service";
import {SearchInfo} from "@mnema/_models/Info";
import {ToastService} from "@mnema/_services/toast.service";
import {DownloadModalComponent} from "@mnema/page/_components/download-modal/download-modal.component";
import {PageService} from "@mnema/_services/page.service";
import {FormControlDefinition, FormDefinition} from "@mnema/generic-form/form";
import {
  MetadataProvider,
  MetadataSearchResult,
  MetadataService
} from "@mnema/features/monitored-series/metadata.service";
import {PagedList} from "@mnema/_models/paged-list";
import {SubscriptionExternalUrlPipe} from "@mnema/_pipes/subscription-external-url.pipe";
import {SentenceCasePipe} from "@mnema/_pipes/sentence-case.pipe";
import {FormService} from "@mnema/_services/form.service";
import {GenericFormModalComponent} from "@mnema/generic-form/generic-form-modal/generic-form-modal.component";
import {FormGroup, NonNullableFormBuilder} from "@angular/forms";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {GenericFormFactoryService} from "@mnema/generic-form/generic-form-factory.service";
import {Tracker} from "@mnema/shared/data-structures/tracker";

@Component({
  selector: 'app-monitored-series',
  standalone: true,
  imports: [CommonModule, MonitoredChapterStatusPipe, ProviderNamePipe, TagBadgeComponent, ContentFormatPipe, FormatPipe, TranslocoDirective, UtcToLocalTimePipe, BadgeComponent, SubscriptionExternalUrlPipe, SentenceCasePipe, NgbTooltip, RouterLink],
  templateUrl: './monitored-series.component.html',
  styleUrl: './monitored-series.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MonitoredSeriesComponent implements OnInit {

  private readonly chapterStatusPipe = new MonitoredChapterStatusPipe();

  private readonly monitoredSeriesService = inject(MonitoredSeriesService);
  private readonly metadataService = inject(MetadataService);
  private readonly transLoco = inject(TranslocoService);
  private readonly modalService = inject(ModalService);
  private readonly signalR = inject(SignalRService);
  private readonly route = inject(ActivatedRoute);
  private readonly toastR = inject(ToastService);
  private readonly pageService = inject(PageService);
  private readonly router = inject(Router);
  private readonly formService = inject(FormService);
  private readonly genericFormFactoryService = inject(GenericFormFactoryService);
  private readonly fb = inject(NonNullableFormBuilder);

  private readonly data = toSignal(this.route.data);

  searchInfoTemplate = viewChild.required<TemplateRef<any>>('searchInfoPreview');
  metadataEditorHeader = viewChild.required<TemplateRef<any>>('metadataEditorHeader');
  coverPreviewTemplate = viewChild.required<TemplateRef<any>>('coverPreview');

  private selectedChapter = signal<MonitoredChapter | null>(null);

  protected readonly Format = Format;
  protected readonly MonitoredChapterStatus = MonitoredChapterStatus;

  protected failedCovers = signal<Set<string>>(new Set());
  protected coverUpdateInProgress = signal(false);
  protected combineInProgress = signal(false);
  protected readonly selectedChapters = new Tracker<MonitoredChapter, string>(chapter => chapter.id);

  protected series = linkedSignal(() => this.data()!['series'] as MonitoredSeries);
  protected provider = computed(() => this.series().provider);
  private editFileMetadataFormDefinition = signal<FormDefinition | null>(null);
  protected metadataFormDefinition = signal<FormControlDefinition[]>([]);
  protected metadata = computed(() => {
    const m = this.series().metadata;

    if (this.series().hardcoverId) {
      m["hardcover_series_id"] = [this.series().hardcoverId];
    }

    if (this.series().mangaBakaId) {
      m["manga_baka_id"] = [this.series().mangaBakaId]
    }

    if (this.series().externalId) {
      m["external_id"] = [this.series().externalId];
    }

    if (this.series().titleOverride) {
      m["title_override"] = [this.series().titleOverride];
    }

    m["format"] = [this.series().format + ''];
    m["contentFormat"] = [this.series().contentFormat + ''];
    m["monitored_series_id"] = [this.series().id];

    if (!!m["ignore_non_matched_volumes"]) {
      m["ignore_non_matched_volumes"] = ["true"];
    }

    return m;
  });

  protected missingHardcoverId = computed(() => !this.series().hardcoverId);
  protected missingMangabakaId = computed(() => !this.series().mangaBakaId);

  protected activeChapterTab = signal<'chapters' | 'unmatched'>('chapters');
  private chapterTabInitialized = false;


  constructor() {
    this.signalR.events$.pipe(
      filter(e => e.type === EventType.MetadataRefreshed),
      map(e => e.data.seriesId as string),
      filter(id => id === this.series().id),
      switchMap(() => this.monitoredSeriesService.get(this.series().id)),
      tap(series => this.series.set(series))
    ).subscribe();

    effect(() => {
      this.pageService.metadata(this.provider()).pipe(
        tap(m => this.metadataFormDefinition.set(m))
      ).subscribe();
    });

    effect(() => {
      const s = this.series();
      if (s && !this.chapterTabInitialized) {
        this.chapterTabInitialized = true;
        if (s.chapters.length === 0 && s.unMatchedChapters.length > 0) {
          this.activeChapterTab.set('unmatched');
        }
      }
    });
  }

  ngOnInit() {
    this.formService.getEditFileMetadataForm().pipe(
      tap(f => this.editFileMetadataFormDefinition.set(f))
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

  protected chapterCoverUrl(chapter: MonitoredChapter): string | null {
    const useArchiveCover = this.series().format === Format.Archive
      && chapter.status === MonitoredChapterStatus.Available
      && !!chapter.filePath
      && !this.failedCovers().has(chapter.id);

    if (useArchiveCover) {
      return this.monitoredSeriesService.coverUrl(this.series().id, chapter.id, chapter.lastModifiedUtc);
    }

    return chapter.coverUrl ?? null;
  }

  protected onCoverError(chapterId: string) {
    this.failedCovers.update(failed => {
      if (failed.has(chapterId)) return failed;

      const next = new Set(failed);
      next.add(chapterId);
      return next;
    });
  }

  protected updateCover(chapter: MonitoredChapter) {
    if (this.coverUpdateInProgress()) return;

    this.coverUpdateInProgress.set(true);
    this.monitoredSeriesService.covers(this.series().id).pipe(
      switchMap(covers => {
        if (covers.length === 0) {
          this.toastR.warningLoco('monitored-series-detail.no-covers');
          return EMPTY;
        }

        const match = covers.find(c => this.sameVolume(c.volume, chapter.volume));
        if (match) {
          return this.applyCovers([chapter], match);
        }

        return this.pickCover(covers).pipe(
          switchMap(cover => this.applyCovers([chapter], cover)),
        );
      }),
      finalize(() => this.coverUpdateInProgress.set(false)),
    ).subscribe();
  }

  protected bulkUpdateCovers() {
    const chapters = this.selectedChapters.items();
    if (chapters.length === 0) return;

    const volume = this.normalizeVolume(chapters[0].volume);
    if (chapters.some(c => this.normalizeVolume(c.volume) !== volume)) {
      this.toastR.errorLoco('monitored-series-detail.same-volume-required');
      return;
    }

    if (this.coverUpdateInProgress()) return;

    this.coverUpdateInProgress.set(true);
    this.monitoredSeriesService.covers(this.series().id).pipe(
      switchMap(covers => {
        if (covers.length === 0) {
          this.toastR.warningLoco('monitored-series-detail.no-covers');
          return EMPTY;
        }

        const match = covers.find(c => this.sameVolume(c.volume, chapters[0].volume));
        if (match) {
          return this.applyCovers(chapters, match);
        }

        return this.pickCover(covers).pipe(
          switchMap(cover => this.applyCovers(chapters, cover)),
        );
      }),
      finalize(() => this.coverUpdateInProgress.set(false)),
    ).subscribe();
  }

  protected combineChapters() {
    const chapters = this.selectedChapters.items();
    if (chapters.length < 2) {
      this.toastR.errorLoco('monitored-series-detail.combine-min-two');
      return;
    }

    const volume = chapters[0].volume;
    if (!volume?.trim()) {
      this.toastR.errorLoco('monitored-series-detail.combine-volume-required');
      return;
    }

    const normalizedVolume = this.normalizeVolume(volume);
    if (chapters.some(c => this.normalizeVolume(c.volume) !== normalizedVolume)) {
      this.toastR.errorLoco('monitored-series-detail.combine-same-volume');
      return;
    }

    if (this.combineInProgress()) return;

    this.combineInProgress.set(true);
    this.modalService.confirm$({
      question: this.transLoco.translate('monitored-series-detail.confirm-combine', {
        count: chapters.length,
        volume: volume,
      }),
    }, true).pipe(
      switchMap(() => this.monitoredSeriesService.combine(this.series().id, chapters.map(c => c.id))),
      switchMap(() => this.monitoredSeriesService.get(this.series().id)),
      tap(series => {
        this.series.set(series);
        this.clearChapterSelection();
        this.toastR.successLoco('monitored-series-detail.combined');
      }),
      finalize(() => this.combineInProgress.set(false)),
    ).subscribe();
  }

  protected toggleChapter(chapter: MonitoredChapter) {
    this.selectedChapters.toggle(chapter);
  }

  protected isChapterSelected(chapterId: string): boolean {
    return this.selectedChapters.lookup().has(chapterId);
  }

  protected clearChapterSelection() {
    this.selectedChapters.reset();
  }

  private pickCover(covers: Cover[]) {
    const [modal, component] = this.modalService.open(ListSelectModalComponent<Cover>, {
      size: "lg", centered: true
    });

    component.title.set(this.transLoco.translate('monitored-series-detail.select-cover'));
    component.inputItems.set(covers.map(cover => ({
      label: cover.volume
        ? this.transLoco.translate('monitored-series-detail.volume-label', {volume: cover.volume})
        : this.transLoco.translate('monitored-series-detail.no-volume'),
      value: cover,
    })));
    component.itemTemplate.set(this.coverPreviewTemplate());
    component.requireConfirmation.set(true);
    component.isSelectionValidFunc.set(selection => selection.length === 1);

    return this.modalService.onClose$<Cover>(modal, true);
  }

  private applyCovers(chapters: MonitoredChapter[], cover: Cover) {
    return this.monitoredSeriesService.updateCovers(this.series().id, chapters.map(c => c.id), cover.url).pipe(
      switchMap(() => this.monitoredSeriesService.get(this.series().id)),
      tap(series => {
        this.series.set(series);
        this.failedCovers.update(failed => {
          if (chapters.every(c => !failed.has(c.id))) return failed;

          const next = new Set(failed);
          chapters.forEach(c => next.delete(c.id));
          return next;
        });
        this.clearChapterSelection();
        this.toastR.successLoco('monitored-series-detail.cover-updated');
      }),
    );
  }

  private sameVolume(first: string, second: string): boolean {
    if (!first?.trim() || !second?.trim()) return false;

    return this.normalizeVolume(first) === this.normalizeVolume(second);
  }

  private normalizeVolume(volume: string): string {
    const parsed = parseFloat(volume);

    return isNaN(parsed) ? (volume ?? '').trim().toLowerCase() : parsed.toString();
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
        if (results.totalCount == 0) {
          this.toastR.warningLoco('monitored-series-detail.no-results');
          return EMPTY;
        }

        const [modal, component] = this.modalService.open(ListSelectModalComponent, {
          size: "lg", centered: true
        });
        component.title.set(this.transLoco.translate('monitored-series-detail.select-search-result'));
        component.inputItems.set(results.items.map(si => ({label: si.name, value: si, url: si.url})));
        component.itemsBeforeVirtual.set(8);
        component.requireConfirmation.set(true);

        return this.modalService.onClose$<SearchInfo>(modal)
      }),
      switchMap(selection => {
        const [modal, component] = this.modalService.open(DownloadModalComponent, {
          size: "xl", centered: true
        });

        component.info.set(selection);
        component.defaultDir.set(this.series().baseDir);
        component.rootDir.set('');
        component.metadataFormDefinition.set(this.metadataFormDefinition());
        component.metadata.set(this.metadata());

        return this.modalService.onClose$(modal);
      }),
      tap(() => this.toastR.infoLoco('monitored-series-detail.download-started', {name: this.series().title}))
    ).subscribe();
  }

  download() {
    const [_, component] = this.modalService.open(DownloadModalComponent, {
      size: "xl", centered: true
    });

    component.info.set({
      id: this.series().externalId,
      downloadUrl: "",
      name: this.series().title,
      description: "",
      size: "",
      tags: [],
      imageUrl: this.series().coverUrl ?? '',
      url: this.series().refUrl ?? '',
      provider: this.series().provider,
    });
    component.defaultDir.set(this.series().baseDir);
    component.rootDir.set('');
    component.metadataFormDefinition.set(this.metadataFormDefinition());
    component.metadata.set(this.metadata());
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
    const [modal, component] = this.modalService.open(EditMonitoredSeriesModalComponent, DefaultModalOptions);
    component.series.set(this.series());

    this.modalService.onClose$(modal)
      .pipe(
        switchMap(() => this.monitoredSeriesService.get(this.series().id)),
        tap(series => this.series.set(series)),
      ).subscribe();
  }

  delete() {
    this.modalService.confirm$({
      question: this.transLoco.translate('monitored-series-detail.confirm-delete', {name: this.series().title})
    }, true).pipe(
      switchMap(() => this.monitoredSeriesService.delete(this.series().id)),
      tap(() => {
        this.toastR.errorLoco('monitored-series-detail.deleted', {name: this.series().title});
        this.router.navigateByUrl('monitored-series').catch(console.error);
      }),
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

  protected searchHardcover() {
    let title = this.series().titleOverride;
    if (!title) {
      title = this.series().title;
    }

    this.metadataService.search(MetadataProvider.Hardcover, title, 0, 20).pipe(
      switchMap(results => this.promptForChoice(results)),
      map(sr => sr.id),
      switchMap(id => {
        const series = {...this.series()};
        series.hardcoverId = id;
        return this.monitoredSeriesService.update(series).pipe(
          switchMap(() => this.monitoredSeriesService.get(series.id)),
        );
      }),
      tap(mSeries => this.series.set(mSeries)),
    ).subscribe();
  }

  protected searchMangabaka() {
    let title = this.series().titleOverride;
    if (!title) {
      title = this.series().title;
    }

    this.metadataService.search(MetadataProvider.Mangabaka, title, 0, 20).pipe(
      switchMap(results => this.promptForChoice(results)),
      map(sr => sr.id),
      switchMap(id => {
        const series = {...this.series()};
        series.mangaBakaId = id;
        return this.monitoredSeriesService.update(series).pipe(
          switchMap(() => this.monitoredSeriesService.get(series.id)),
        );
      }),
      tap(mSeries => this.series.set(mSeries)),
    ).subscribe();
  }

  private promptForChoice(results: PagedList<MetadataSearchResult>) {
    if (results.totalCount == 0) {
      this.toastR.warningLoco('monitored-series-detail.no-results');
      return EMPTY;
    }

    const [modal, component] = this.modalService.open(ListSelectModalComponent<MetadataSearchResult>, {
      size: "lg", centered: true
    });

    component.title.set(this.transLoco.translate('monitored-series-detail.select-search-result'));
    component.inputItems.set(results.items.map(sr => ({label: sr.title, value: sr, url: sr.refUrl ?? undefined})));
    component.itemsBeforeVirtual.set(8);
    component.requireConfirmation.set(true);
    component.itemTemplate.set(this.searchInfoTemplate());
    component.disableHover.set(true);

    return this.modalService.onClose$<MetadataSearchResult>(modal, true);
  }

  protected openBaseDir() {
    if (!this.series().titleOverride) return;

    this.modalService.getDirectory$(this.series().baseDir + '/' + this.series().titleOverride, {
      showFiles: true, copy: false, filter: true, create: false
    }).subscribe();
  }

  protected editFile(path: string, chapter: MonitoredChapter | null = null) {
    if (!path) return;

    const form = this.editFileMetadataFormDefinition();
    if (!form) return;

    this.selectedChapter.set(chapter)

    this.monitoredSeriesService.getFileInfo(this.series().id, path).pipe(
      switchMap((fileInfo) => {
        const [modal, component] = this.modalService.open(GenericFormModalComponent, {
          ...DefaultModalOptions,
          backdrop: 'static',
        });

        let title = translate(form.key + '.modal-title', {seriesName: this.series().title});
        if (fileInfo.volume) {
          title += ` Vol. ${fileInfo.volume}`;
        }
        if (fileInfo.chapter) {
          title += ` Ch. ${fileInfo.chapter}`;
        }
        if (fileInfo.path) {
          title += ` (${fileInfo.path})`
        }

        component.title.set(title);
        component.translationKey.set(form.key);
        component.formDefinition.set(form);
        component.double.set(false);
        component.showChanges.set(true);
        component.initialValue.set(fileInfo.metadata ?? {});
        component.descriptionTemplate.set(this.metadataEditorHeader());

        return this.modalService.onClose$<FileMetadata>(modal);
      }),
      switchMap(f => this.monitoredSeriesService.writeFileMetadata(this.series().id, path, f)),
      tap(() => {
        this.toastR.successLoco('edit-file-metadata.metadata-updated', {}, {path: path});
        this.selectedChapter.set(null);
      }),
    ).subscribe();
  }

  protected loadUpstreamMetadata(form: FormGroup) {
    const formDefinition = this.editFileMetadataFormDefinition();
    if (!formDefinition) return;

    this.monitoredSeriesService.getChapterMetadata(this.series().id, this.selectedChapter()?.id).pipe(
      tap(metadata => {
        this.genericFormFactoryService.extendFormGroupForValue(form, formDefinition.controls, metadata, this.fb);
        form.setValue(metadata);
      })
    ).subscribe();
  }

}
