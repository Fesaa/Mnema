import {ChangeDetectionStrategy, Component, computed, inject,} from '@angular/core';
import {NgTemplateOutlet} from "@angular/common";
import {BadgeComponent} from "../../shared/_component/badge/badge.component";
import {ContentStatePipe} from "../../_pipes/content-state.pipe";
import {ContentTitlePipe} from "../../_pipes/content-title.pipe";
import {SpeedPipe} from "../../_pipes/speed.pipe";
import {SpeedTypePipe} from "../../_pipes/speed-type.pipe";
import {TableComponent} from "../../shared/_component/table/table.component";
import {TimePipe} from "../../_pipes/time.pipe";
import {TranslocoDirective, TranslocoPipe} from "@jsverse/transloco";
import {InfoStat} from "../../_models/stats";
import {ContentAction} from "../../_models/content-action";
import {LoadingSpinnerComponent} from "../../shared/_component/loading-spinner/loading-spinner.component";
import {ModalService} from "../../_services/modal.service";
import {ContentService} from "../../_services/content.service";
import {ToastService} from "../../_services/toast.service";
import {ContentPickerDialogComponent} from "../_components/content-picker-dialog/content-picker-dialog.component";
import {
  DownloadFilesModalComponent
} from "../_components/download-files-modal/download-files-modal.component";
import {DefaultModalOptions} from "../../_models/default-modal-options";
import {
  ManualContentAddModalComponent
} from "../_components/manual-content-add-modal/manual-content-add-modal.component";
import {StopRequest} from "../../_models/search";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {SuggestionDashboardComponent} from "../_components/suggestion-dashboard/suggestion-dashboard.component";
import {ActiveDownloadsService} from "./active-downloads.service";
import {
  DeleteDownloadModalComponent
} from "@mnema/dashboard/_components/delete-download-modal/delete-download-modal.component";
import {switchMap, tap} from "rxjs";

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
    TranslocoPipe,
    LoadingSpinnerComponent,
    NgbTooltip,
    SuggestionDashboardComponent,
    NgTemplateOutlet
  ],
  templateUrl: './active-downloads.component.html',
  styleUrl: './active-downloads.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActiveDownloadsComponent {

  private readonly modalService = inject(ModalService);
  private readonly contentService = inject(ContentService);
  private readonly toastService = inject(ToastService);
  private readonly contentTitle = inject(ContentTitlePipe);
  private readonly service = inject(ActiveDownloadsService);

  private readonly actionConfig: Partial<Record<ContentAction, {icon: string; tooltip: string; buttonClass: string}>> = {
    [ContentAction.Browse]: {icon: 'fa fa-folder', tooltip: 'dashboard.table.actions.browse', buttonClass: 'btn btn-primary'},
    [ContentAction.SetContent]: {icon: 'fa fa-cart-plus', tooltip: 'dashboard.table.actions.set-content', buttonClass: 'btn btn-primary'},
    [ContentAction.MarkReady]: {icon: 'fa fa-check', tooltip: 'dashboard.table.actions.mark-ready', buttonClass: 'btn btn-secondary'},
    [ContentAction.EditMarkers]: {icon: 'fa fa-pencil', tooltip: 'dashboard.table.actions.edit-markers', buttonClass: 'btn btn-secondary'},
    [ContentAction.Stop]: {icon: 'fa fa-trash', tooltip: 'dashboard.table.actions.stop', buttonClass: 'btn btn-error'},
  };

  loading = this.service.loading;
  dashboardItems = computed(() => this.service.items().sort((a, b) => {
    if (a.contentState == b.contentState) {
      return a.id.localeCompare(b.id)
    }

    // Bigger first
    return b.contentState - a.contentState;
  }));

  async stop(info: InfoStat) {
    const [modal, component] = this.modalService.open(DeleteDownloadModalComponent, DefaultModalOptions);
    component.info.set(info);

    this.modalService.onClose$<boolean>(modal).pipe(
      switchMap(removeFromDownloadClient => {
        const req: StopRequest = {
          provider: info.provider,
          delete: true,
          id: info.id,
          deleteFromDownloadClient: removeFromDownloadClient,
        }

        return this.contentService.stop(req)
      }),
      tap(() => this.toastService.successLoco("dashboard.toasts.stopped-success", {}, {title: this.contentTitle.transform(info.name)}))
    ).subscribe()
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

  editMarkers(info: InfoStat) {
    const [_, component] = this.modalService.open(DownloadFilesModalComponent, DefaultModalOptions);
    component.info.set(info);
  }

  itemTrackBy(idx: number, item: InfoStat): string {
    return `${item.id}`
  }

  getConfig(action: ContentAction) {
    return this.actionConfig[action];
  }

  runAction(action: ContentAction, info: InfoStat): void {
    switch (action) {
      case ContentAction.Browse:
        void this.browse(info);
        break;
      case ContentAction.SetContent:
        this.pickContent(info);
        break;
      case ContentAction.MarkReady:
        this.markReady(info);
        break;
      case ContentAction.EditMarkers:
        this.editMarkers(info);
        break;
      case ContentAction.Stop:
        void this.stop(info);
        break;
    }
  }

  manualAdd() {
    this.modalService.open(ManualContentAddModalComponent, DefaultModalOptions);
  }
}
