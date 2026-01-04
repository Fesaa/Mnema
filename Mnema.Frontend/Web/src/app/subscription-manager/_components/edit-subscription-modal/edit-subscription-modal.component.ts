import {ChangeDetectionStrategy, Component, computed, inject, model, OnInit, signal} from '@angular/core';
import {RefreshFrequencies, Subscription} from "../../../_models/subscription";
import {DownloadMetadata, Provider} from "../../../_models/page";
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
import {NgTemplateOutlet} from "@angular/common";
import {FormControlDefinition, FormDefinition, FormType} from "../../../generic-form/form";
import {tap} from "rxjs";
import {GenericFormComponent} from "../../../generic-form/generic-form.component";
import {GenericFormFactoryService} from "../../../generic-form/generic-form-factory.service";

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
    RefreshFrequencyPipe,
    NgTemplateOutlet,
    GenericFormComponent
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
  private readonly genericFormFactoryService = inject(GenericFormFactoryService);

  subscription = model.required<Subscription>();
  providers = model.required<Provider[]>();
  metadata = model.required<DownloadMetadata>();

  formDefinition = signal<FormDefinition | undefined>(undefined);
  optionsFormDefinition = computed(() => {
    const form = this.formDefinition();
    if (!form) return null;

    return {
      key: form.key,
      descriptionKey: '',
      controls: this.metadata().definitions.filter(d => !d.advanced),
    }
  });
  advancedFormDefinition = computed(() => {
    const form = this.formDefinition();
    if (!form) return null;

    return {
      key: form.key,
      descriptionKey: '',
      controls: this.metadata().definitions.filter(d => d.advanced),
    }
  });

  activeTab: 'general' | 'options' | 'advanced' = 'general';

  subscriptionForm = new FormGroup({});

  ngOnInit(): void {
    this.subscriptionService.getForm().pipe(
      tap(form => this.formDefinition.set(form)),
    ).subscribe();
  }

  async pickDirectory() {
    const dir = await this.modalService.getDirectory('', {
      copy: true,
      filter: true,
      create: true,
      showFiles: false,
    });

    if (dir) {
      (this.subscriptionForm.get('baseDir') as unknown as FormControl<string>)?.setValue(dir);
    }
  }

  close() {
    this.modal.dismiss();
  }

  save() {
    const sub = {
      ...this.subscription(),
      ...this.genericFormFactoryService.adjustForGenericMetadata(this.subscriptionForm.value),
    };

    console.log(this.genericFormFactoryService.adjustForGenericMetadata(this.subscriptionForm.value));

    const actions$ = this.subscription().id === ''
      ? this.subscriptionService.new(sub)
      : this.subscriptionService.update(sub);
    const kind = this.subscription().id === '' ? 'new' : 'update';

    actions$.subscribe({
      next: () => {
        this.toastService.successLoco(`subscriptions.toasts.${kind}.success`, {name: sub.title});
      },
      error: err => {
        this.toastService.errorLoco(`subscriptions.toasts.${kind}.error`, {name: sub.title}, {msg: err.error.message});
      }
    }).add(() => this.close());
  }

}
