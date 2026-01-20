import { Pipe, PipeTransform } from '@angular/core';
import {MonitoredChapterStatus} from "@mnema/features/monitored-series/monitored-series.service";

@Pipe({
  name: 'monitoredChapterStatus',
})
export class MonitoredChapterStatusPipe implements PipeTransform {

  transform(value: MonitoredChapterStatus): string {
    switch (value) {
      case MonitoredChapterStatus.NotMonitored:
        return 'Not Monitored';
      case MonitoredChapterStatus.Missing:
        return 'Missing';
      case MonitoredChapterStatus.Upcoming:
        return 'Upcoming';
      case MonitoredChapterStatus.Importing:
        return 'Importing';
      case MonitoredChapterStatus.Available:
        return 'Available';
      default:
        return 'Unknown';
    }
  }

}
