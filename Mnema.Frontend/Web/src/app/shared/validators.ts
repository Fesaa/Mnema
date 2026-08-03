import {AbstractControl, AsyncValidatorFn, ValidationErrors, ValidatorFn} from "@angular/forms";
import {HttpClient} from "@angular/common/http";
import {environment} from "@env/environment";
import {switchMap, timer} from "rxjs";


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

  static serverSideValidation(
    httpClient: HttpClient,
    urlPath: string
  ): AsyncValidatorFn {
    return (control: AbstractControl) => {
      return timer(200).pipe(
        switchMap(() =>
          httpClient.post<ValidationErrors | null>(
            environment.apiUrl + urlPath,
            { formValue: control.value }
          )
        )
      );
    };
  }

}
