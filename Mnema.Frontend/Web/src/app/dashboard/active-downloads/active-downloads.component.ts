import {ChangeDetectionStrategy, Component, computed, inject,} from '@angular/core';
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
import {ContentService} from "../../_services/content.service";
import {ToastService} from "../../_services/toast.service";
import {ContentPickerDialogComponent} from "../_components/content-picker-dialog/content-picker-dialog.component";
import {DefaultModalOptions} from "../../_models/default-modal-options";
import {
  ManualContentAddModalComponent
} from "../_components/manual-content-add-modal/manual-content-add-modal.component";
import {StopRequest} from "../../_models/search";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {SuggestionDashboardComponent} from "../_components/suggestion-dashboard/suggestion-dashboard.component";
import {ActiveDownloadsService} from "./active-downloads.service";

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
export class ActiveDownloadsComponent {

  private readonly modalService = inject(ModalService);
  private readonly contentService = inject(ContentService);
  private readonly toastService = inject(ToastService);
  private readonly contentTitle = inject(ContentTitlePipe);
  private readonly service = inject(ActiveDownloadsService);

  loading = this.service.loading;
  dashboardItems = computed(() => this.service.items().sort((a, b) => {
    if (a.contentState == b.contentState) {
      return a.id.localeCompare(b.id)
    }

    // Bigger first
    return b.contentState - a.contentState;
  }));

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
