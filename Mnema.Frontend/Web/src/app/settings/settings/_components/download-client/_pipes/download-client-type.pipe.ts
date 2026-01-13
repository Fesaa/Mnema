import { Pipe, PipeTransform } from '@angular/core';
import {DownloadClientType} from "../download-client.service";
import {translate} from "@jsverse/transloco";

@Pipe({
  name: 'downloadClientType',
})
export class DownloadClientTypePipe implements PipeTransform {

  transform(value: DownloadClientType): string {
    switch (value) {
      case DownloadClientType.QBitTorrent:
        return translate('download-client-pipe.qbit-torrent');
    }
  }

}
