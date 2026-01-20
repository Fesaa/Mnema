import { Pipe, PipeTransform } from '@angular/core';
import {PersonRole} from "@mnema/page/_components/series-info/_types";
import {translate} from "@jsverse/transloco";

@Pipe({
  name: 'personRole',
})
export class PersonRolePipe implements PipeTransform {

  transform(value: PersonRole): string {
    switch (value) {
      case PersonRole.Writer:
        return translate('person-role-pipe.writer');
      case PersonRole.Penciller:
        return translate('person-role-pipe.penciller');
      case PersonRole.Inker:
        return translate('person-role-pipe.inker');
      case PersonRole.Colorist:
        return translate('person-role-pipe.colorist');
      case PersonRole.Letterer:
        return translate('person-role-pipe.letterer');
      case PersonRole.CoverArtist:
        return translate('person-role-pipe.cover-artist');
      case PersonRole.Editor:
        return translate('person-role-pipe.editor');
      case PersonRole.Translator:
        return translate('person-role-pipe.translator');
      case PersonRole.Publisher:
        return translate('person-role-pipe.publisher');
      case PersonRole.Imprint:
        return translate('person-role-pipe.imprint');
      case PersonRole.Character:
        return translate('person-role-pipe.character');
    }
  }

}
