import {Pipe, PipeTransform} from '@angular/core';
import {ConnectionType} from "../connection.service";

@Pipe({
  name: 'connectionType',
  standalone: true
})
export class ConnectionTypePipe implements PipeTransform {

  transform(type: ConnectionType): string {
    switch (type) {
      case ConnectionType.Discord:
        return 'Discord';
      case ConnectionType.Kavita:
        return 'Kavita';
      case ConnectionType.Native:
        return 'Native';
      default:
        return 'Unknown';
    }
  }
}
