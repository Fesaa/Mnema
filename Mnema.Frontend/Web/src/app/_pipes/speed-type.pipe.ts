import {Pipe, PipeTransform} from '@angular/core';
import {SpeedType} from "../_models/stats";

@Pipe({
  name: 'speedType',
  standalone: true
})
export class SpeedTypePipe implements PipeTransform {

  transform(value: SpeedType): string {
    switch (value) {
      case SpeedType.BYTES:
        return "MB/s";
      case SpeedType.VOLUMES:
        return "volumes/s"
      case SpeedType.IMAGES:
        return "images/s"
    }
  }

}
