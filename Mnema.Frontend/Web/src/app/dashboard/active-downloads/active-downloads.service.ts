import {DestroyRef, inject, Injectable, signal} from '@angular/core';
import {ContentService} from "../../_services/content.service";
import {EventType, SignalRService} from "../../_services/signal-r.service";
import {ContentState, InfoStat} from "../../_models/stats";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {ContentProgressUpdate, ContentSizeUpdate, ContentStateUpdate, DeleteContent} from "../../_models/signalr";

@Injectable({
  providedIn: 'root',
})
export class ActiveDownloadsService {
  private readonly contentService = inject(ContentService);
  private readonly signalR = inject(SignalRService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly items = signal<InfoStat[]>([]);

  constructor() {
    this.reload();

    this.signalR.events$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
      switch (event.type) {
        case EventType.ContentStateUpdate:
          this.updateState(event.data as ContentStateUpdate);
          break;
        case EventType.ContentSizeUpdate:
          this.updateSize(event.data as ContentSizeUpdate);
          break;
        case EventType.DeleteContent:
          this.items.update(x => x.filter(item => item.id !== (event.data as DeleteContent).contentId))
          break;
        case EventType.ContentProgressUpdate:
          this.updateProgress(event.data as ContentProgressUpdate);
          break;
        case EventType.AddContent:
          this.addContent(event.data as InfoStat);
          break;
        case EventType.ContentInfoUpdate:
          this.updateInfo(event.data as InfoStat);
          break;
        case EventType.BulkContentInfoUpdate:
          (event.data as InfoStat[]).forEach(i => this.updateInfo(i));
          break;
      }
    });
  }

  reload() {
    this.loading.set(true);
    this.contentService.infoStats().subscribe(info => {
      this.loading.set(false);
      this.items.set(info || []);
    });
  }

  private updateInfo(info: InfoStat) {
    this.items.update(x => x.map(i => {
      if (i.id !== info.id) {
        return i;
      }
      return info;
    }))
  }

  private addContent(event: InfoStat) {
    if (this.items().find(item => item.id === event.id) !== undefined ) {
      return;
    }

    this.items.update(x => {
      x.push(event);
      return x;
    });
  }

  private updateSize(event: ContentSizeUpdate) {
    this.items.update(x => x.map(item => {
      if (item.id == event.contentId) {
        item.size = event.size;
      }

      return item;
    }));
  }

  private updateProgress(event: ContentProgressUpdate) {
    this.items.update(x => x.map(item => {
      if (item.id == event.contentId) {
        item.progress = event.progress;
        item.estimated = event.estimated;
        item.speed = event.speed;
        item.speedType = event.speed_type;
        // Sometimes the updateState seems to get out of sync, if content is sending progress updates, it's downloading
        item.contentState = ContentState.Downloading;
      }

      return item;
    }));
  }

  private updateState(event: ContentStateUpdate) {
    this.items.update(x => x.map(item => {
      if (item.id == event.contentId) {
        item.contentState = event.contentState;
      }

      return item;
    }));
  }
}
