import {Pipe, PipeTransform} from '@angular/core';
import {ModifierType} from "../_models/page";
import {translate} from "@jsverse/transloco";

@Pipe({
  name: 'modifierType'
})
export class ModifierTypePipe implements PipeTransform {

  transform(value: ModifierType): string {
    switch (value) {
      case ModifierType.MULTI:
        return translate('modifier-type-pipe.multi')
      case ModifierType.DROPDOWN:
        return translate('modifier-type-pipe.dropdown')
      case ModifierType.SWITCH:
        return translate('modifier-type-pipe.switch')
    }
  }

}
