import {ChangeDetectionStrategy, Component, inject, model, OnInit} from '@angular/core';
import {RefreshFrequencies, Subscription} from "../../../_models/subscription";
import {DownloadMetadata, DownloadMetadataDefinition, DownloadMetadataFormType, Provider} from "../../../_models/page";
import {FormControl, FormGroup, ReactiveFormsModule} from '@angular/forms';
import {ToastService} from "../../../_services/toast.service";
import {ModalService} from "../../../_services/modal.service";
import {NgbActiveModal, NgbNav, NgbNavContent, NgbNavItem, NgbNavLink, NgbNavOutlet} from "@ng-bootstrap/ng-bootstrap";
import {SettingsItemComponent} from "../../../shared/form/settings-item/settings-item.component";
import {DefaultValuePipe} from "../../../_pipes/default-value.pipe";
import {TranslocoDirective} from "@jsverse/transloco";
import {ProviderNamePipe} from "../../../_pipes/provider-name.pipe";
import {RefreshFrequencyPipe} from "../../../_pipes/refresh-frequency.pipe";
import {SubscriptionExternalUrlPipe} from "../../../_pipes/subscription-external-url.pipe";
import {SubscriptionService} from "../../../_services/subscription.service";

@Component({
  selector: 'app-edit-subscription-modal',
  imports: [
    ReactiveFormsModule,
    SettingsItemComponent,
    DefaultValuePipe,
    TranslocoDirective,
    NgbNav,
    NgbNavContent,
    NgbNavLink,
    NgbNavItem,
    NgbNavOutlet,
    ProviderNamePipe,
    RefreshFrequencyPipe
  ],
  templateUrl: './edit-subscription-modal.component.html',
  styleUrl: './edit-subscription-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditSubscriptionModalComponent implements OnInit {

  private readonly toastService = inject(ToastService);
  private readonly modalService = inject(ModalService);
  private readonly subscriptionService = inject(SubscriptionService);
  private readonly modal = inject(NgbActiveModal);
  private readonly externalUrlPipe = inject(SubscriptionExternalUrlPipe);

  subscription = model.required<Subscription>();
  providers = model.required<Provider[]>();
  metadata = model.required<DownloadMetadata>();

  activeTab: 'general' | 'extra' = 'general';

  subscriptionForm = new FormGroup({});

  ngOnInit(): void {
    const subscription = this.subscription();
    const metadata = this.metadata();

    this.subscriptionForm.addControl("ID", new FormControl(subscription.id));
    this.subscriptionForm.addControl('provider', new FormControl(subscription.provider));
    this.subscriptionForm.addControl('contentId', new FormControl(subscription.contentId));
    this.subscriptionForm.addControl('refreshFrequency', new FormControl(subscription.refreshFrequency));
    this.subscriptionForm.addControl('title', new FormControl(subscription.title));
    this.subscriptionForm.addControl('description', new FormControl(subscription.description));
    this.subscriptionForm.addControl('baseDir', new FormControl(subscription.baseDir));
    this.subscriptionForm.addControl('lastDownloadDir', new FormControl(subscription.lastDownloadDir));

    const metadataFormGroup = new FormGroup<any>({
      startImmediately: new FormControl(subscription.metadata.startImmediately),
    });

    for (let definition of metadata.definitions) {
      metadataFormGroup.addControl(definition.key, new FormControl(this.getDefaultValue(subscription, definition)));
    }

    this.subscriptionForm.addControl('metadata', metadataFormGroup);
  }

  private getDefaultValue(sub: Subscription, def: DownloadMetadataDefinition) {
    const values = (sub.metadata.extra || {})[def.key]
    const value = (values && values.length > 0) ? values[0] : def.defaultOption;

    switch (def.formType) {
      case DownloadMetadataFormType.SWITCH:
        return value.toLowerCase() === 'true';
      case DownloadMetadataFormType.TEXT:
        return value;
      case DownloadMetadataFormType.DROPDOWN:
        return value;
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

  async pickDirectory() {
    const dir = await this.modalService.getDirectory('', {
      copy: true,
      filter: true,
      create: true,
      showFiles: false,
    });

    if (dir) {
      (this.subscriptionForm.get('info.baseDir') as unknown as FormControl<string>)?.setValue(dir);
    }
  }

  close() {
    this.modal.close();
  }

  private packData() {
    const data = this.subscriptionForm.value as Subscription;

    // Get extra's in the expected format
    const extras: { [key: string]: string[] } = {}
    Object.keys(data.metadata)
      .filter(key => key !== 'startImmediately' &&
        (data.metadata as any)[key] !== undefined &&
        (data.metadata as any)[key] !== null)
      .forEach(key => {
        const val = (data.metadata as any)[key];
        extras[key] = Array.isArray(val) ? val.map(v => v+'') : [val+''];
      });

    data.metadata = {
      startImmediately: true,
      extra: extras,
    };

    data.provider = parseInt(data.provider+'');
    data.refreshFrequency = parseInt(data.refreshFrequency+'');

    return data;
  }

  save() {
    const sub = this.packData();

    const actions$ = this.subscription().id === -1
      ? this.subscriptionService.new(sub)
      : this.subscriptionService.update(sub);
    const kind = this.subscription().id === -1 ? 'new' : 'update';

    actions$.subscribe({
      next: () => {
        this.toastService.successLoco(`subscriptions.toasts.${kind}.success`, {name: sub.title});
      },
      error: err => {
        this.toastService.errorLoco(`subscriptions.toasts.${kind}.error`, {name: sub.title}, {msg: err.error.message});
      }
    }).add(() => this.close())
  }

  openExternal() {
    const data = this.subscriptionForm.value as any;
    const contentId = data.contentId;
    const provider = parseInt(data.provider)

    window.open(this.externalUrlPipe.transform(contentId, provider), '_blank', 'noopener noreferrer');
  }

  protected readonly DownloadMetadataFormType = DownloadMetadataFormType;
  protected readonly RefreshFrequencies = RefreshFrequencies;

}
