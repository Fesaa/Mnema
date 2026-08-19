import {Component, computed, inject} from '@angular/core';
import {PageService} from "../../../_services/page.service";
import {RouterLink} from "@angular/router";
import {dropAnimation} from "../../../_animations/drop-animation";
import {ToastService} from "../../../_services/toast.service";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {ModalService} from "../../../_services/modal.service";
import {ManualContentAddModalComponent} from "../manual-content-add-modal/manual-content-add-modal.component";
import {DefaultModalOptions} from "../../../_models/default-modal-options";
import {UpdateBadgeDirective} from "../../../_directives/update-badge.directive";

interface Option {
  id: string,
  title: string,
  action?: () => void,
  version?: string,
}

@Component({
  selector: 'app-suggestion-dashboard',
  imports: [
    RouterLink,
    TranslocoDirective,
    UpdateBadgeDirective
  ],
  templateUrl: './suggestion-dashboard.component.html',
  styleUrl: './suggestion-dashboard.component.scss',
  animations: [dropAnimation]
})
export class SuggestionDashboardComponent {

  private readonly pageService = inject(PageService);
  private readonly modalService = inject(ModalService);
  private readonly toastService = inject(ToastService);

  options = computed(() => {
    const options: Option[] = this.pageService.pages().map(p => {
      return {
        id: p.id,
        title: p.title,
      };
    });

    if (options.length === 0) {
      return options // Ensure we display the load defaults
    }

    options.push({
      id: '',
      title: translate('dashboard.manual-add.title'),
      action: this.manualAdd.bind(this),
      version: "0.3.6",
    })

    return options;
  })

  manualAdd() {
    this.modalService.open(ManualContentAddModalComponent, DefaultModalOptions);
  }

  loadDefault() {
    this.pageService.loadDefault().subscribe({
      next: () => {
        this.pageService.refreshPages().subscribe();
      },
      error: (err) => {
        this.toastService.genericError(err.error.message);
      }
    })
  }

}
