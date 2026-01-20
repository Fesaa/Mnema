import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {ModalService} from "../../../_services/modal.service";
import {DownloadModalComponent} from "../../../page/_components/download-modal/download-modal.component";
import {DefaultModalOptions} from "../../../_models/default-modal-options";
import {FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators} from "@angular/forms";
import {AllProviders, Provider} from "../../../_models/page";
import {PageService} from "../../../_services/page.service";
import {catchError, of, tap} from "rxjs";
import {SettingsItemComponent} from "../../../shared/form/settings-item/settings-item.component";
import {TranslocoDirective} from "@jsverse/transloco";
import {DefaultValuePipe} from "../../../_pipes/default-value.pipe";
import {ProviderNamePipe} from "../../../_pipes/provider-name.pipe";

@Component({
  selector: 'app-manual-content-add-modal',
  imports: [
    ReactiveFormsModule,
    SettingsItemComponent,
    TranslocoDirective,
    DefaultValuePipe,
    ProviderNamePipe
  ],
  templateUrl: './manual-content-add-modal.component.html',
  styleUrl: './manual-content-add-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManualContentAddModalComponent {

  private readonly pageService = inject(PageService);
  private readonly modal = inject(NgbActiveModal);
  private readonly modalService = inject(ModalService);
  private readonly fb = inject(NonNullableFormBuilder);

  form!: FormGroup<{
    id: FormControl<string>,
    name: FormControl<string>
    provider: FormControl<Provider>
  }>;

  constructor() {
    this.form = this.fb.group({
      id: this.fb.control<string>('', [Validators.required]),
      name: this.fb.control<string>(''),
      provider: this.fb.control<Provider>(Provider.NYAA),
    });
  }

  close() {
    this.modal.close();
  }

  submit() {
    if (!this.form.valid) return

    const data = this.form.getRawValue();
    this.pageService.metadata(data.provider).pipe(
      tap(metadata => {
        this.close();

        const [_, component] = this.modalService.open(DownloadModalComponent, DefaultModalOptions);
        component.metadata.set(metadata);
        component.defaultDir.set('');
        component.rootDir.set('');
        component.info.set({
          id: data.id,
          name: data.name ?? data.id,
          downloadUrl: '',
          description: "",
          size: "",
          tags: [],
          imageUrl: "",
          url: "",
          provider: data.provider,
        });
      }),
      catchError(() => {
        this.close();
        return of(null);
      })
    ).subscribe();
  }

  protected readonly AllProviders = AllProviders;
}
