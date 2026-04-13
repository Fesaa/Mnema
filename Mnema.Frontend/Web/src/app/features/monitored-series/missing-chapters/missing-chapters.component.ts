import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {MonitoredChapter, MonitoredSeriesService} from "@mnema/features/monitored-series/monitored-series.service";
import { TableComponent } from "@mnema/shared/_component/table/table.component";
import {TranslocoModule} from "@jsverse/transloco";
import {RouterLink} from "@angular/router";

@Component({
  selector: 'app-missing-chapters',
  standalone: true,
  imports: [
    CommonModule,
    TranslocoModule,
    TableComponent,
    RouterLink
  ],
  templateUrl: './missing-chapters.component.html',
  styleUrl: './missing-chapters.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MissingChaptersComponent {
  private readonly monitoredSeriesService = inject(MonitoredSeriesService);

  pageLoader = (pageNumber: number, pageSize: number) => {
    return this.monitoredSeriesService.missingChapters(pageNumber, pageSize);
  }

  trackBy(index: number, item: MonitoredChapter): string {
    return item.id;
  }
}
