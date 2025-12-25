import {Pipe, PipeTransform} from '@angular/core';
import {RefreshFrequency} from "../_models/subscription";

@Pipe({
  name: 'refreshFrequency',
  standalone: true
})
export class RefreshFrequencyPipe implements PipeTransform {

  transform(value: RefreshFrequency): string {
    switch (value) {
      case RefreshFrequency.Day:
        return '1 Day';
      case RefreshFrequency.Week:
        return '1 Week';
      case RefreshFrequency.Month:
        return '1 Month';
      default:
        return 'Unknown';
    }
  }

}
