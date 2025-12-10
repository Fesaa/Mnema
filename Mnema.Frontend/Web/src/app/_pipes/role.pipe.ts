import { Pipe, PipeTransform } from '@angular/core';
import {Role} from "../_models/user";
import {translate} from "@jsverse/transloco";

@Pipe({
  name: 'role'
})
export class RolePipe implements PipeTransform {

  transform(value: Role): string {
    return translate('role-pipe.'+value);
  }

}
