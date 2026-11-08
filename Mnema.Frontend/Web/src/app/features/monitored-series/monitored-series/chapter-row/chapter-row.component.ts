import {ChangeDetectionStrategy, Component, input, output} from '@angular/core';
import {NgClass} from '@angular/common';
import {TranslocoDirective} from '@jsverse/transloco';
import {NgbTooltip} from '@ng-bootstrap/ng-bootstrap';
import {
  Format,
  MonitoredChapter,
  MonitoredChapterStatus,
  MonitoredSeries,
} from '@mnema/features/monitored-series/monitored-series.service';
import {
  MonitoredChapterStatusPipe
} from '@mnema/features/monitored-series/pipes/monitored-chapter-status.pipe';
import {UtcToLocalTimePipe} from '@mnema/_pipes/utc-to-local.pipe';

@Component({
  selector: 'app-chapter-row',
  imports: [NgClass, TranslocoDirective, NgbTooltip, MonitoredChapterStatusPipe, UtcToLocalTimePipe],
  templateUrl: './chapter-row.component.html',
  styleUrl: './chapter-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true
})
export class ChapterRowComponent {
  readonly series = input.required<MonitoredSeries>();
  readonly chapter = input.required<MonitoredChapter>();
  readonly coverUrl = input<string | null>(null);
  readonly selectable = input(false);
  readonly selected = input(false);
  readonly coverUpdateInProgress = input(false);

  readonly selectedChange = output<MonitoredChapter>();
  readonly updateCover = output<MonitoredChapter>();
  readonly editFile = output<MonitoredChapter>();
  readonly setStatus = output<MonitoredChapter>();
  readonly coverError = output<string>();

  protected readonly Format = Format;
  protected readonly MonitoredChapterStatus = MonitoredChapterStatus;

  protected getStatusClass(status: MonitoredChapterStatus): string {
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
}
