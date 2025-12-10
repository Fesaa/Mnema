import {Pipe, PipeTransform} from '@angular/core';
import {SpeedType} from "../_models/stats";

@Pipe({
  name: 'speed',
  standalone: true
})
export class SpeedPipe implements PipeTransform {

  transform(speed: number, type: SpeedType): number {
    switch (type) {
      case SpeedType.BYTES:
        return +(speed / (1024 * 1024)).toFixed(2);
    }
    return speed
  }

}
