import {ChangeDetectionStrategy, Component, computed, inject, model, OnInit} from '@angular/core';
import {DownloadMetadata, DownloadMetadataDefinition, DownloadMetadataFormType, Page} from "../../../_models/page";
import {TranslocoDirective} from "@jsverse/transloco";
import {NgbActiveModal, NgbNav, NgbNavContent, NgbNavItem, NgbNavLink, NgbNavOutlet} from "@ng-bootstrap/ng-bootstrap";
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from "@angular/forms";
import {SearchInfo} from "../../../_models/Info";
import {SettingsItemComponent} from "../../../shared/form/settings-item/settings-item.component";
import {ModalService} from "../../../_services/modal.service";
import {DownloadRequest} from "../../../_models/search";
import {NgTemplateOutlet} from "@angular/common";
import {DefaultValuePipe} from "../../../_pipes/default-value.pipe";
import {ContentService} from "../../../_services/content.service";
import {ToastService} from "../../../_services/toast.service";
import {SettingsSwitchComponent} from "../../../shared/form/settings-switch/settings-switch.component";

@Component({
  selector: 'app-download-modal',
  imports: [
    TranslocoDirective,
    NgbNav,
    NgbNavItem,
    NgbNavLink,
    NgbNavContent,
    NgbNavOutlet,
    SettingsItemComponent,
    ReactiveFormsModule,
    NgTemplateOutlet,
    DefaultValuePipe,
    SettingsSwitchComponent
  ],
  templateUrl: './download-modal.component.html',
  styleUrl: './download-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DownloadModalComponent implements OnInit {

  private readonly toastService = inject(ToastService);
  private readonly modalService = inject(ModalService);
  private readonly contentService = inject(ContentService)
  private readonly modal = inject(NgbActiveModal);

  info = model.required<SearchInfo>();
  defaultDir = model.required<string>();
  rootDir = model.required<string>();
  dirs = model<string[]>([]);
  metadata = model.required<DownloadMetadata>();

  generalDef = computed(() =>
    this.metadata().definitions.filter(d => !d.advanced))
  advancedDef = computed(() =>
    this.metadata().definitions.filter(d => d.advanced))

  activeTab: 'general' | 'advanced' = 'general';

  downloadForm = new FormGroup({})

  ngOnInit(): void {
    const info = this.info();
    const metadata = this.metadata();

    // Not used in form, but required in download request
    this.downloadForm.addControl('id', new FormControl(info.InfoHash));
    this.downloadForm.addControl('provider', new FormControl(info.Provider));
    this.downloadForm.addControl('title', new FormControl(info.Name, []));

    this.downloadForm.addControl('dir', new FormControl(this.defaultDir(), [Validators.required]));

    const downloadMetadata = new FormGroup<any>({
      startImmediately: new FormControl(true),
    });

    for (let def of metadata.definitions) {
      downloadMetadata.addControl(def.key, new FormControl(this.getDefaultValue(def)));
    }

    this.downloadForm.addControl('downloadMetadata', downloadMetadata);
  }

  private getDefaultValue(def: DownloadMetadataDefinition) {
    switch (def.formType) {
      case DownloadMetadataFormType.SWITCH:
        return Boolean(def.defaultOption);
      case DownloadMetadataFormType.TEXT:
        return def.defaultOption;
      case DownloadMetadataFormType.DROPDOWN:
        return def.defaultOption;
    }
    return null;
  }

  getValue(def: DownloadMetadataDefinition, key: string) {
    if (!def.options) return key;

    const opt = def.options.find(d => d.key === key);
    if (opt) {
      return opt.value;
    }
    return key;
  }

  packData() {
    const data = this.downloadForm.value as DownloadRequest;

    // Get extra's in the expected format
    const extras: { [key: string]: string[] } = {}
    Object.keys(data.downloadMetadata)
      .filter(key => key !== 'startImmediately' &&
        (data.downloadMetadata as any)[key] !== undefined &&
        (data.downloadMetadata as any)[key] !== null)
      .forEach(key => {
        const val = (data.downloadMetadata as any)[key];
        extras[key] = Array.isArray(val) ? val.map(v => v+'') : [val+''];
      });

    data.downloadMetadata = {
      startImmediately: data.downloadMetadata.startImmediately,
      extra: extras,
    }

    return data;
  }

  async pickDirectory() {
    const dir = await this.modalService.getDirectory(this.rootDir(), {
      copy: true,
      filter: true,
      create: true,
      showFiles: false,
    });

    if (dir) {
      (this.downloadForm.get('dir') as unknown as FormControl<string>)?.setValue(dir);
    }
  }


  close() {
    this.modal.close();
  }

  download() {
    const req = this.packData();

    this.contentService.download(req).subscribe({
      next: () => {
        this.toastService.successLoco("page.download-dialog.toasts.download-success", {}, {name: this.info().Name});
      },
      error: (err) => {
        this.toastService.genericError(err.error.message);
      }
    }).add(() => {
      this.close()
    })

  }

  protected readonly DownloadMetadataFormType = DownloadMetadataFormType;
}
