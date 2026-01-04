import {AbstractControl, ValidatorFn} from "@angular/forms";


export class MnemaValidators {

  static startsWith(prefix: string): ValidatorFn {
    return (control: AbstractControl) => {
      const value = control.value;
      if (typeof value !== 'string')
        return null;

      if (value.startsWith(prefix))
        return null;

      return { 'startsWith': { 'prefix': prefix } };
    }
  }

  static isUrl(control: AbstractControl) {
    const value = control.value;
    if (typeof value !== 'string')
      return null;

    try {
      new URL(value);
    } catch (e) {
      return { 'isUrl': {}}
    }

    return null;
  }

}
