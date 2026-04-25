import {ChangeDetectionStrategy, Component, computed, inject, model, OnInit, signal} from '@angular/core';
import {TranslocoDirective} from "@jsverse/transloco";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {FormGroup, ReactiveFormsModule} from "@angular/forms";
import {SearchInfo} from "../../../_models/Info";
import {DownloadRequest, MetadataBag} from "../../../_models/search";
import {ContentService} from "../../../_services/content.service";
import {ToastService} from "../../../_services/toast.service";
import {FormControlDefinition, FormDefinition} from "../../../generic-form/form";
import {catchError, tap} from "rxjs";
import {GenericFormComponent} from "../../../generic-form/generic-form.component";
import {GenericFormFactoryService} from "../../../generic-form/generic-form-factory.service";

@Component({
  selector: 'app-download-modal',
  imports: [
    TranslocoDirective,
    ReactiveFormsModule,
    GenericFormComponent
  ],
  templateUrl: './download-modal.component.html',
  styleUrl: './download-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DownloadModalComponent implements OnInit {

  private readonly toastService = inject(ToastService);
  private readonly contentService = inject(ContentService)
  private readonly modal = inject(NgbActiveModal);
  private readonly genericFormFactoryService = inject(GenericFormFactoryService);

  info = model.required<SearchInfo>();
  defaultDir = model.required<string>();
  rootDir = model.required<string>();
  metadataFormDefinition = model.required<FormControlDefinition[]>();
  metadata = model<MetadataBag>({});
  saving = signal(false);

  private formDefinition = signal<FormDefinition | undefined>(undefined);
  generalFormDefinition = computed(() => {
    const form = this.formDefinition();
    if (!form) return null;

    return {
      key: form.key,
      descriptionKey: form.descriptionKey,
      controls: [
        ...form.controls,
        ...this.metadataFormDefinition().filter(d => !d.advanced)
      ],
    };
  })
  advancedFormDefinition = computed(() => {
    const form = this.formDefinition();
    if (!form) return null;

    return {
      key: form.key,
      descriptionKey: form.descriptionKey,
      controls: this.metadataFormDefinition().filter(d => d.advanced)
    }
  })

  baseRequest = computed<DownloadRequest>(() => ({
    id: this.info().id,
    provider: this.info().provider,
    downloadUrl: this.info().downloadUrl,
    title: this.info().name,
    startImmediately: true,
    metadata: this.metadata(),
    baseDir: this.defaultDir(),
  }));

  activeTab: 'general' | 'advanced' = 'general';

  downloadForm = new FormGroup({})

  ngOnInit(): void {
    this.contentService.getForm().pipe(
      tap(form => this.formDefinition.set(form)),
    ).subscribe();
  }


  close() {
    this.modal.dismiss();
  }

  download() {
    const form = this.genericFormFactoryService.adjustForGenericMetadata(this.downloadForm.value);


    const req = {
      ...this.baseRequest(),
      ...form
    };

    // Ensure metadata is merged, with values in the form taking priority
    req.metadata = {
      ...this.metadata(),
      ...form.metadata,
    }

    this.contentService.download(req).pipe(
      tap(() => this.saving.set(false)),
      tap(() => this.toastService.successLoco("page.download-dialog.toasts.download-success", {}, {name: this.info().name})),
      catchError(err => {
        this.toastService.genericError(err.error.message);
        throw err;
      }),
      tap(() => this.close())
    ).subscribe();

  }

}
