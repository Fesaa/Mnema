import {Pipe, PipeTransform} from '@angular/core';

@Pipe({
  name: 'time',
  standalone: true
})
export class TimePipe implements PipeTransform {

  transform(value?: number): string {
    if (!value) {
      return 'unknown';
    }

    if (value === 0) {
      return 'now';
    }

    const seconds = Math.floor(value % 60);
    const minutes = Math.floor((value / 60) % 60);
    const hours = Math.floor((value / 3600) % 24);
    const days = Math.floor(value / 86400);

    let result = '';
    if (days > 0) {
      result += `${days}d `;
    }
    if (hours > 0) {
      result += `${hours}h `;
    }
    if (minutes > 0) {
      result += `${minutes}m `;
    }
    if (seconds > 0) {
      result += `${seconds}s`;
    }
    return result;
  }

}
