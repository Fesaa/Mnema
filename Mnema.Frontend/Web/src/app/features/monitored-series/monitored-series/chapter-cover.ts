import {
  Format,
  MonitoredChapter,
  MonitoredChapterStatus,
  MonitoredSeries,
  MonitoredSeriesService,
} from '@mnema/features/monitored-series/monitored-series.service';

export function resolveChapterCoverUrl(
  service: MonitoredSeriesService,
  series: MonitoredSeries,
  chapter: MonitoredChapter,
  failedCoverIds: ReadonlySet<string>,
): string | null {
  const useArchiveCover = series.format === Format.Archive
    && chapter.status === MonitoredChapterStatus.Available
    && !!chapter.filePath
    && !failedCoverIds.has(chapter.id);

  if (useArchiveCover) {
    return service.coverUrl(series.id, chapter.id, chapter.lastModifiedUtc);
  }

  return chapter.coverUrl ?? null;
}
