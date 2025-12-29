import {Component, computed, effect, inject, signal} from '@angular/core';
import {Page, Provider} from "../../../../_models/page";
import {PageService} from "../../../../_services/page.service";
import {RouterLink} from "@angular/router";
import {dropAnimation} from "../../../../_animations/drop-animation";
import {ReactiveFormsModule} from "@angular/forms";
import {AccountService} from "../../../../_services/account.service";
import {ToastService} from "../../../../_services/toast.service";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {CdkDragDrop, CdkDragHandle, moveItemInArray} from "@angular/cdk/drag-drop";
import {TableComponent} from "../../../../shared/_component/table/table.component";
import {ModalService} from "../../../../_services/modal.service";
import {EditPageModalComponent} from "./_components/edit-page-modal/edit-page-modal.component";
import {DefaultModalOptions} from "../../../../_models/default-modal-options";

@Component({
  selector: 'app-pages-settings',
  imports: [
    RouterLink,
    ReactiveFormsModule,
    TranslocoDirective,
    CdkDragHandle,
    TableComponent,
  ],
  templateUrl: './pages-settings.component.html',
  styleUrl: './pages-settings.component.scss',
  animations: [dropAnimation]
})
export class PagesSettingsComponent {

  private readonly modalService = inject(ModalService);
  private readonly toastService = inject(ToastService);
  private readonly pageService = inject(PageService);
  private readonly accountService = inject(AccountService);

  user = computed(() => this.accountService.currentUser());
  pages = signal<Page[]>([]);
  loading = signal(true);

  constructor() {
    effect(() => {
      this.pages.set(this.pageService.pages());
    });
  }

  edit(page: Page | null) {
    const [modal, component] = this.modalService.open(EditPageModalComponent, DefaultModalOptions);
    component.page.set(page ?? {
      id: "",
      customRootDir: '',
      title: '',
      provider: Provider.BATO,
      icon: '',
      sortValue: 0,
    });
  }

  async remove(page: Page) {
    if (!await this.modalService.confirm({
      question: translate("settings.pages.confirm-delete", {title: page.title})
    })) {
      return;
    }

    this.pageService.removePage(page.id).subscribe({
      next: () => {
        this.toastService.successLoco("settings.pages.toasts.delete.success", {}, {title: page.title});
        this.pageService.refreshPages().subscribe();
      },
      error: (err) => {
        this.toastService.genericError(err.error.message);
      }
    });
  }

  drop($event: CdkDragDrop<any, any>) {
    const pages = [...this.pages()];
    const copy = [...pages];

    // Assume no error will occur
    moveItemInArray(pages, $event.previousIndex, $event.currentIndex);
    this.pages.set(pages);

    this.pageService.orderPages(pages.map(p => p.id)).subscribe({
      next: () => {
        this.pageService.refreshPages().subscribe();
      },
      error: (err) => {
        this.toastService.genericError(err.error.message);
        this.pages.set(copy);
      }
    });
  }

  trackBy(idx: number, page: Page) {
    return `${page.id}`
  }
}
