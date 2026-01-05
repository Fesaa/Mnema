import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal
} from '@angular/core';
import {BadgeComponent} from "../../shared/_component/badge/badge.component";
import {ContentStatePipe} from "../../_pipes/content-state.pipe";
import {ContentTitlePipe} from "../../_pipes/content-title.pipe";
import {SpeedPipe} from "../../_pipes/speed.pipe";
import {SpeedTypePipe} from "../../_pipes/speed-type.pipe";
import {TableComponent} from "../../shared/_component/table/table.component";
import {TimePipe} from "../../_pipes/time.pipe";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {ContentState, InfoStat} from "../../_models/stats";
import {LoadingSpinnerComponent} from "../../shared/_component/loading-spinner/loading-spinner.component";
import {ModalService} from "../../_services/modal.service";
import {NavService} from "../../_services/nav.service";
import {ContentService} from "../../_services/content.service";
import {ToastService} from "../../_services/toast.service";
import {EventType, SignalRService} from "../../_services/signal-r.service";
import {ContentProgressUpdate, ContentSizeUpdate, ContentStateUpdate, DeleteContent} from "../../_models/signalr";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {ContentPickerDialogComponent} from "../_components/content-picker-dialog/content-picker-dialog.component";
import {DefaultModalOptions} from "../../_models/default-modal-options";
import {
  ManualContentAddModalComponent
} from "../_components/manual-content-add-modal/manual-content-add-modal.component";
import {StopRequest} from "../../_models/search";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {SuggestionDashboardComponent} from "../_components/suggestion-dashboard/suggestion-dashboard.component";

@Component({
  selector: 'app-active-downloads',
  imports: [
    BadgeComponent,
    ContentStatePipe,
    ContentTitlePipe,
    SpeedPipe,
    SpeedTypePipe,
    TableComponent,
    TimePipe,
    TranslocoDirective,
    LoadingSpinnerComponent,
    NgbTooltip,
    SuggestionDashboardComponent
  ],
  templateUrl: './active-downloads.component.html',
  styleUrl: './active-downloads.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActiveDownloadsComponent implements OnInit {

  private readonly modalService = inject(ModalService);
  private readonly navService = inject(NavService);
  private readonly contentService = inject(ContentService);
  private readonly toastService = inject(ToastService);
  private readonly contentTitle = inject(ContentTitlePipe);
  private readonly signalR = inject(SignalRService);
  private readonly destroyRef = inject(DestroyRef);

  loading = signal(true);
  items = signal<InfoStat[]>([]);
  dashboardItems = computed(() => this.items().sort((a, b) => {
    if (a.contentState == b.contentState) {
      return a.id.localeCompare(b.id)
    }

    // Bigger first
    return b.contentState - a.contentState;
  }));

  ngOnInit(): void {
    this.contentService.infoStats().subscribe(info => {
      this.loading.set(false);
      this.items.set(info || []);
    })

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
      }
    })
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

  async stop(info: InfoStat) {
    if (!await this.modalService.confirm({
      question: translate("dashboard.confirm-stop", {name: this.contentTitle.transform(info.name)})
    })) {
      return;
    }

    const req: StopRequest = {
      provider: info.provider,
      delete: true,
      id: info.id,
    }

    this.contentService.stop(req).subscribe({
      next: () => {
        this.toastService.successLoco("dashboard.toasts.stopped-success", {}, {title: this.contentTitle.transform(info.name)});
      },
      error: (err) => {
        this.toastService.genericError(err.error.message);
      }
    })
  }

  async browse(info: InfoStat) {
    await this.modalService.getDirectory(info.downloadDir, {showFiles: true});
  }

  markReady(info: InfoStat) {
    this.contentService.startDownload(info.provider, info.id).subscribe({
      next: () => {
        this.toastService.successLoco("dashboard.toasts.mark-ready.success")
      },
      error: (err) => {
        this.toastService.genericError(err.error.message);
      }
    })
  }

  pickContent(info: InfoStat) {
    const [_, component] = this.modalService.open(ContentPickerDialogComponent, DefaultModalOptions);
    component.info.set(info);
  }

  itemTrackBy(idx: number, item: InfoStat): string {
    return `${item.id}`
  }

  manualAdd() {
    this.modalService.open(ManualContentAddModalComponent, DefaultModalOptions);
  }

  protected readonly ContentState = ContentState;
}
