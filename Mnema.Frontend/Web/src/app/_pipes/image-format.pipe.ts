import {Pipe, PipeTransform} from '@angular/core';
import {ImageFormat} from "../_models/preferences";

@Pipe({
  name: 'imageFormat',
})
export class ImageFormatPipe implements PipeTransform {

  transform(value: ImageFormat): string {
    switch (value) {
      case ImageFormat.Upstream:
        return "Upstream"
      case ImageFormat.Webp:
        return "Webp";
    }
  }

}
