import {Pipe, PipeTransform} from '@angular/core';
import {ExternalConnectionEvent} from "../external-connection.service";
import {translate} from "@jsverse/transloco";

@Pipe({
  name: 'externalConnectionEvent',
  standalone: true
})
export class ExternalConnectionEventPipe implements PipeTransform {

  transform(event: ExternalConnectionEvent): string {
    switch (event) {
      case ExternalConnectionEvent.DownloadStarted:
        return translate('settings.external-connections.shared.event.DownloadStarted');
      case ExternalConnectionEvent.DownloadFinished:
        return translate('settings.external-connections.shared.event.DownloadFinished');
      case ExternalConnectionEvent.DownloadFailure:
        return translate('settings.external-connections.shared.event.DownloadFailure');
      default:
        return 'Unknown';
    }
  }
}
