import {Pipe, PipeTransform} from '@angular/core';
import {ConnectionEvent} from "../connection.service";
import {translate} from "@jsverse/transloco";

@Pipe({
  name: 'ConnectionEvent',
  standalone: true
})
export class ConnectionEventPipe implements PipeTransform {

  transform(event: ConnectionEvent): string {
    switch (event) {
      case ConnectionEvent.TooManyForAutomatedDownload:
        return translate('external-connections-event-types-pipe.TooManyForAutomatedDownload');
      case ConnectionEvent.DownloadClientEvents:
        return translate('external-connections-event-types-pipe.DownloadClientEvents');
      case ConnectionEvent.GenericDownloadInfo:
        return translate('external-connections-event-types-pipe.GenericDownloadInfo');
      case ConnectionEvent.SubscriptionExhausted:
        return translate('external-connections-event-types-pipe.SubscriptionExhausted');
      case ConnectionEvent.SeriesMonitored:
        return translate('external-connections-event-types-pipe.SeriesMonitored');
      case ConnectionEvent.SeriesUnmonitored:
        return translate('external-connections-event-types-pipe.SeriesUnmonitored');
      case ConnectionEvent.DownloadStarted:
        return translate('external-connections-event-types-pipe.DownloadStarted');
      case ConnectionEvent.DownloadFinished:
        return translate('external-connections-event-types-pipe.DownloadFinished');
      case ConnectionEvent.DownloadFailure:
        return translate('external-connections-event-types-pipe.DownloadFailure');
      default:
        return 'Unknown';
    }
  }
}
