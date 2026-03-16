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
      case ConnectionEvent.SubscriptionExhausted:
        return translate('settings.connections.shared.event.SubscriptionExhausted');
      case ConnectionEvent.SeriesMonitored:
        return translate('settings.connections.shared.event.SeriesMonitored');
      case ConnectionEvent.SeriesUnmonitored:
        return translate('settings.connections.shared.event.SeriesUnmonitored');
      case ConnectionEvent.DownloadStarted:
        return translate('settings.connections.shared.event.DownloadStarted');
      case ConnectionEvent.DownloadFinished:
        return translate('settings.connections.shared.event.DownloadFinished');
      case ConnectionEvent.DownloadFailure:
        return translate('settings.connections.shared.event.DownloadFailure');
      default:
        return 'Unknown';
    }
  }
}
