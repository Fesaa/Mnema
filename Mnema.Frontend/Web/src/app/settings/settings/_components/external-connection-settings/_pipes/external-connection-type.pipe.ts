import {Pipe, PipeTransform} from '@angular/core';
import {ExternalConnectionType} from "../external-connection.service";

@Pipe({
  name: 'externalConnectionType',
  standalone: true
})
export class ExternalConnectionTypePipe implements PipeTransform {

  transform(type: ExternalConnectionType): string {
    switch (type) {
      case ExternalConnectionType.Discord:
        return 'Discord';
      case ExternalConnectionType.Kavita:
        return 'Kavita';
      default:
        return 'Unknown';
    }
  }
}
