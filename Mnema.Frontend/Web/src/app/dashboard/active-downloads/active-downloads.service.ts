import {DestroyRef, effect, inject, Injectable, signal} from '@angular/core';
import {ContentService} from "../../_services/content.service";
import {EventType, SignalRService} from "../../_services/signal-r.service";
import {ContentState, InfoStat} from "../../_models/stats";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {
  ContentProgressUpdate,
  ContentSizeUpdate,
  ContentStateUpdate,
  DeleteContent
} from "../../_models/signalr";
import {SettingsService} from "@mnema/_services/settings.service";
import {environment} from "@env/environment";

@Injectable({
  providedIn: 'root',
})
export class ActiveDownloadsService {
  private readonly contentService = inject(ContentService);
  private readonly signalR = inject(SignalRService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly settingService = inject(SettingsService);

  private readonly debug = false;

  readonly loading = signal(true);
  readonly items = signal<InfoStat[]>([]);

  constructor() {
    this.log('Service initialized');

    this.signalR.events$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(event => {
        this.log('SignalR event received', event);

        switch (event.type) {
          case EventType.ContentStateUpdate:
            this.updateState(event.data as ContentStateUpdate);
            break;

          case EventType.ContentSizeUpdate:
            this.updateSize(event.data as ContentSizeUpdate);
            break;

          case EventType.DeleteContent:
            this.deleteContent(event.data as DeleteContent);
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

          case EventType.RefreshDashboard:
            this.log('Refreshing dashboard');
            this.reload();
            break;
        }
      });

    effect(() => {
      if (this.settingService.isAuthenticated()) {
        this.reload();
      }
    });
  }

  reload() {
    this.log('Reloading content');

    this.loading.set(true);

    this.contentService.infoStats().subscribe(info => {
      this.loading.set(false);

      const items = info || [];
      this.log(`Loaded ${items.length} items`);

      this.items.set(items);
    });
  }

  private updateInfo(info: InfoStat) {
    this.items.update(items => {
      const item = items.find(i => i.id === info.id);

      if (!item) {
        this.log('updateInfo: item not found', info);
        return items;
      }

      return items.map(i => i.id === info.id ? info : i);
    });
  }

  private addContent(event: InfoStat) {
    this.items.update(items => {
      const existing = items.find(item => item.id === event.id);

      if (existing) {
        this.log('addContent: item already exists', event);
        return items;
      }

      this.log('addContent: adding item', event);

      return [...items, event];
    });
  }

  private deleteContent(event: DeleteContent) {
    this.items.update(items => {
      const exists = items.find(item => item.id === event.contentId);

      if (!exists) {
        this.log('deleteContent: item not found', event);
        return items;
      }

      this.log('deleteContent: removing item', event.contentId);

      return items.filter(item => item.id !== event.contentId);
    });
  }

  private updateSize(event: ContentSizeUpdate) {
    this.items.update(items => {
      const item = items.find(i => i.id === event.contentId);

      if (!item) {
        this.log('updateSize: item not found', event);
        return items;
      }

      item.size = event.size;

      this.log('updateSize', {
        id: event.contentId,
        size: event.size
      });

      return [...items];
    });
  }

  private updateProgress(event: ContentProgressUpdate) {
    this.items.update(items => {
      const item = items.find(i => i.id === event.contentId);

      if (!item) {
        this.log('updateProgress: item not found', event);
        return items;
      }

      item.progress = event.progress;
      item.estimated = event.estimated;
      item.speed = event.speed;
      item.speedType = event.speed_type;

      // Sometimes the state update arrives late.
      item.contentState = ContentState.Downloading;

      this.log('updateProgress', {
        id: event.contentId,
        progress: event.progress,
        speed: event.speed,
        estimated: event.estimated
      });

      return [...items];
    });
  }

  private updateState(event: ContentStateUpdate) {
    this.items.update(items => {
      const item = items.find(i => i.id === event.contentId);

      if (!item) {
        this.log('updateState: item not found', event);
        return items;
      }

      this.log('updateState', {
        id: event.contentId,
        state: event.contentState
      });

      item.contentState = event.contentState;

      return [...items];
    });
  }

  private log(message: string, data?: unknown) {
    if (!this.debug || environment.production) {
      return;
    }

    if (data !== undefined) {
      console.debug(`[ActiveDownloadsService] ${message}`, data);
    } else {
      console.debug(`[ActiveDownloadsService] ${message}`);
    }
  }
}
