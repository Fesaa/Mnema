import { Pipe, PipeTransform } from '@angular/core';
import {MetadataProvider} from "@mnema/features/monitored-series/metadata.service";

@Pipe({
  name: 'metadataProvider',
})
export class MetadataProviderPipe implements PipeTransform {

  transform(value: MetadataProvider): string {
    switch (value) {
      case MetadataProvider.Hardcover:
        return "Hardcover";
      case MetadataProvider.Mangabaka:
        return "MangaBaka";
      case MetadataProvider.Upstream:
        return "Upstream";
    }
  }

}
