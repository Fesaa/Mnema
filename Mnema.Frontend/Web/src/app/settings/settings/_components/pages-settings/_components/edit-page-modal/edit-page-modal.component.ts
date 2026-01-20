import {ChangeDetectionStrategy, Component, inject, model, OnInit} from '@angular/core';
import {AllProviders, Page} from "../../../../../../_models/page";
import {NgbActiveModal,} from "@ng-bootstrap/ng-bootstrap";
import {PageService} from "../../../../../../_services/page.service";
import {TranslocoDirective} from "@jsverse/transloco";
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from "@angular/forms";
import {SafeHtmlPipe} from "../../../../../../_pipes/safe-html-pipe";
import {SettingsItemComponent} from "../../../../../../shared/form/settings-item/settings-item.component";
import {DefaultValuePipe} from "../../../../../../_pipes/default-value.pipe";
import {ModalService} from "../../../../../../_services/modal.service";
import {ProviderNamePipe} from "../../../../../../_pipes/provider-name.pipe";
import {ToastService} from "../../../../../../_services/toast.service";

@Component({
  selector: 'app-edit-page-modal',
  imports: [
    TranslocoDirective,
    ReactiveFormsModule,
    SafeHtmlPipe,
    SettingsItemComponent,
    DefaultValuePipe,
    ProviderNamePipe
  ],
  templateUrl: './edit-page-modal.component.html',
  styleUrl: './edit-page-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditPageModalComponent implements OnInit {

  private readonly modalService = inject(ModalService);
  private readonly modal = inject(NgbActiveModal);
  private readonly pageService = inject(PageService);
  private readonly toastService = inject(ToastService);

  page = model.required<Page>();

  pageForm = new FormGroup({});

  ngOnInit(): void {
    const page = this.page();

    this.pageForm.addControl('title', new FormControl(page.title, [Validators.required]));
    this.pageForm.addControl('icon', new FormControl(page.icon, []));
    this.pageForm.addControl('customRootDir', new FormControl(page.customRootDir, []));
    this.pageForm.addControl('provider', new FormControl(page.provider, []));
  }


  async pickCustomRootDir() {
    const dir = await this.modalService.getDirectory('', {showFiles: false, create: true});
    if (dir) {
      (this.pageForm.get('customRootDir') as unknown as FormControl<string>)?.setValue(dir);
    }
  }

  break(s: string) {
    if (s) return s.split(',').filter(s => s.trim() !== '');

    return [];
  }

  close() {
    this.modal.close();
  }

  save() {
    if (!this.pageForm.valid) return;

    const page = this.pageForm.value as any;
    page.id = this.page().id;
    page.sortValue = this.page().sortValue;

    const action$ = this.page().id === ""
      ? this.pageService.new(page)
      : this.pageService.update(page);

    action$.subscribe({
      next: (page) => {
        this.toastService.successLoco("settings.pages.toasts.save.success");
        this.pageService.refreshPages().subscribe();
      },
      error: (error) => {
        this.toastService.errorLoco("settings.pages.toasts.save.error", {}, {msg: error.error.message});
      }
    }).add(() => this.close())
  }

  protected readonly AllProviders = AllProviders;
}
