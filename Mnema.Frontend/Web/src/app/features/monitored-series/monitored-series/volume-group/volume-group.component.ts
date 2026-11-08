import {ChangeDetectionStrategy, Component, computed, inject, input, output, signal} from '@angular/core';
import {TranslocoDirective} from '@jsverse/transloco';
import {NgbTooltip} from '@ng-bootstrap/ng-bootstrap';
import {
  Format,
  MonitoredChapter,
  MonitoredChapterStatus,
  MonitoredSeries,
  MonitoredSeriesService,
} from '@mnema/features/monitored-series/monitored-series.service';
import {resolveChapterCoverUrl} from '../chapter-cover';
import {VolumeGroup} from '../volume-group.model';
import {ChapterRowComponent} from '../chapter-row/chapter-row.component';

@Component({
  selector: 'app-volume-group',
  imports: [TranslocoDirective, NgbTooltip, ChapterRowComponent],
  templateUrl: './volume-group.component.html',
  styleUrl: './volume-group.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true
})
export class VolumeGroupComponent {
  private readonly monitoredSeriesService = inject(MonitoredSeriesService);

  readonly series = input.required<MonitoredSeries>();
  readonly group = input.required<VolumeGroup>();
  readonly failedCoverIds = input<ReadonlySet<string>>(new Set<string>());
  readonly coverUpdateInProgress = input(false);
  readonly combineInProgress = input(false);

  readonly updateCover = output<void>();
  readonly combine = output<void>();
  readonly updateChapterCover = output<MonitoredChapter>();
  readonly editChapterFile = output<MonitoredChapter>();
  readonly setChapterStatus = output<MonitoredChapter>();
  readonly coverError = output<string>();

  protected readonly Format = Format;
  protected expanded = signal(false);

  protected readonly coverUrl = computed(() => {
    const chapter = this.group().coverChapter;
    if (!chapter) return null;

    return resolveChapterCoverUrl(this.monitoredSeriesService, this.series(), chapter, this.failedCoverIds());
  });

  protected readonly canUpdateCover = computed(() => this.series().format === Format.Archive
    && this.group().hasVolume
    && this.group().availableChapters.length > 0);

  protected readonly allChaptersAvailable = computed(() =>
    this.group().chapters.every(chapter => chapter.status === MonitoredChapterStatus.Available || chapter.status === MonitoredChapterStatus.NotMonitored));

  protected readonly canCombine = computed(() => this.series().format === Format.Archive
    && this.group().hasVolume
    && this.allChaptersAvailable()
    && this.group().combineChapters.length >= 2);

  protected readonly isCombined = computed(() => {
    if (this.series().format !== Format.Archive) {
      return false;
    }

    if (!this.allChaptersAvailable()) {
      return false;
    }

    const chapters = this.group().availableChapters.filter(chapter => chapter.status === MonitoredChapterStatus.Available);
    if (chapters.length === 0) {
      return false;
    }

    const firstFilePath = chapters[0].filePath;
    return !chapters.some(chapter => chapter.filePath !== firstFilePath);
  })

  protected chapterCoverUrl(chapter: MonitoredChapter): string | null {
    return resolveChapterCoverUrl(this.monitoredSeriesService, this.series(), chapter, this.failedCoverIds());
  }

  protected toggleExpanded() {
    this.expanded.update(expanded => !expanded);
  }

  protected onCoverError() {
    const chapter = this.group().coverChapter;
    if (chapter) {
      this.coverError.emit(chapter.id);
    }
  }
}
