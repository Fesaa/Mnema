import {ChangeDetectionStrategy, Component, computed, inject, model, OnInit, signal} from '@angular/core';
import {FormsModule} from "@angular/forms";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {TranslocoDirective, TranslocoService} from "@jsverse/transloco";
import {InfoStat} from "../../../_models/stats";
import {ExternalDownloadFile} from "../../../_models/external-download-file";
import {MonitoredChapterOption} from "../../../_models/download-files";
import {UpdateDownloadFileMarkers} from "../../../_models/update-download-file-markers";
import {ContentService} from "../../../_services/content.service";
import {ToastService} from "../../../_services/toast.service";
import {LoadingSpinnerComponent} from "../../../shared/_component/loading-spinner/loading-spinner.component";

type EditableDownloadFile = {
  path: string;
  volumeMarker: string;
  chapterMarker: string;
  selectedChapterId: string;
}

@Component({
  selector: 'app-download-files-modal',
  imports: [FormsModule, TranslocoDirective, LoadingSpinnerComponent],
  templateUrl: './download-files-modal.component.html',
  styleUrl: './download-files-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DownloadFilesModalComponent implements OnInit {

  private readonly contentService = inject(ContentService);
  private readonly toastService = inject(ToastService);
  private readonly modal = inject(NgbActiveModal);
  private readonly transloco = inject(TranslocoService);

  info = model.required<InfoStat>();

  protected files = signal<EditableDownloadFile[]>([]);
  protected monitoredChapters = signal<MonitoredChapterOption[] | null>(null);
  protected loading = signal(true);
  protected submitting = signal(false);
  protected isMonitored = computed(() => this.monitoredChapters() !== null);

  ngOnInit(): void {
    this.contentService.getDownloadFiles(this.info().provider, this.info().id).subscribe({
      next: result => {
        this.monitoredChapters.set(result.monitoredChapters);
        this.files.set(result.files.map(file => this.toEditable(file, result.monitoredChapters ?? [])));
        this.loading.set(false);
      },
      error: err => {
        this.toastService.genericError(err?.error?.message ?? 'Unknown error');
        this.loading.set(false);
      }
    });
  }

  submit(): void {
    const files: UpdateDownloadFileMarkers[] = this.isMonitored()
      ? this.files().map(file => ({
        path: file.path,
        monitoredChapterId: file.selectedChapterId || null,
      }))
      : this.files().map(file => ({
        path: file.path,
        volumeMarker: file.volumeMarker,
        chapterMarker: file.chapterMarker,
      }));

    this.submitting.set(true);
    this.contentService.updateDownloadFileMarkers(this.info().provider, this.info().id, files).subscribe({
      next: () => {
        this.toastService.successLoco('dashboard.marker-editor.toasts.success');
        this.modal.close();
      },
      error: err => {
        this.submitting.set(false);
        this.toastService.genericError(err?.error?.message ?? 'Unknown error');
      }
    });
  }

  close(): void {
    this.modal.dismiss();
  }

  chapterLabel(chapter: MonitoredChapterOption): string {
    const volume = chapter.volume
      ? `${this.transloco.translate('dashboard.marker-editor.volume-short')} ${chapter.volume}`
      : '';
    const chapterMarker = chapter.chapter
      ? `${this.transloco.translate('dashboard.marker-editor.chapter-short')} ${chapter.chapter}`
      : '';

    return [volume, chapterMarker, chapter.title].filter(Boolean).join(' - ');
  }

  private toEditable(file: ExternalDownloadFile, chapters: MonitoredChapterOption[]): EditableDownloadFile {
    const stored = file.monitoredChapterId;
    const selected = stored && chapters.some(c => c.id === stored)
      ? stored
      : this.findChapterId(file, chapters) ?? '';

    return {
      path: file.path,
      volumeMarker: file.volumeMarker ?? '',
      chapterMarker: file.chapterMarker ?? '',
      selectedChapterId: selected,
    };
  }

  private findChapterId(file: ExternalDownloadFile, chapters: MonitoredChapterOption[]): string | null {
    const volume = file.volumeMarker ?? '';
    const chapterMarker = file.chapterMarker ?? '';

    return chapters.find(c => c.volume === volume && c.chapter === chapterMarker)?.id ?? null;
  }
}
