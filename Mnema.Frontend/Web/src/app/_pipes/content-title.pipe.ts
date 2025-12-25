import {Pipe, PipeTransform} from '@angular/core';

@Pipe({
  name: 'contentTitle',
  standalone: true
})
export class ContentTitlePipe implements PipeTransform {

  transform(value: string): string {
    if (!value) return value;
    return value.replace(/[\._-]+/g, ' ');
  }

}
