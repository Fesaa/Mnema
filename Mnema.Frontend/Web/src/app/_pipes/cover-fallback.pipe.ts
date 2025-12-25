import {Pipe, PipeTransform} from '@angular/core';
import {CoverFallbackMethod} from "../_models/preferences";

@Pipe({
  name: 'coverFallback'
})
export class CoverFallbackPipe implements PipeTransform {

  transform(value: CoverFallbackMethod): string {
    switch (value) {
      case CoverFallbackMethod.CoverFallbackFirst:
        return "First";
      case CoverFallbackMethod.CoverFallbackLast:
        return "Last";
      case CoverFallbackMethod.CoverFallbackNone:
        return "None";
    }
  }

}
