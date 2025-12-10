import {Pipe, PipeTransform} from '@angular/core';
import {ContentState} from "../_models/stats";

@Pipe({
  name: 'contentState'
})
export class ContentStatePipe implements PipeTransform {

  transform(value: ContentState): string {
    switch (value) {
      case ContentState.Downloading:
        return "Downloading";
      case ContentState.Loading:
        return "Loading";
      case ContentState.Queued:
        return "Queued";
      case ContentState.Ready:
        return "Ready";
      case ContentState.Waiting:
        return "Waiting";
      case ContentState.Cleanup:
        return "Cleanup";
      default:
        return "Unknown";
    }
  }

}
