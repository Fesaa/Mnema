import {ChangeDetectionStrategy, Component, computed, inject, model, OnInit, signal} from '@angular/core';
import {Subscription} from "../../../_models/subscription";
import {Provider} from "../../../_models/page";
import {FormGroup, ReactiveFormsModule} from '@angular/forms';
import {ToastService} from "../../../_services/toast.service";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {TranslocoDirective} from "@jsverse/transloco";
import {SubscriptionService} from "../../../_services/subscription.service";
import {FormControlDefinition, FormDefinition} from "../../../generic-form/form";
import {tap} from "rxjs";
import {GenericFormComponent} from "../../../generic-form/generic-form.component";
import {GenericFormFactoryService} from "../../../generic-form/generic-form-factory.service";

@Component({
  selector: 'app-edit-subscription-modal',
  imports: [
    ReactiveFormsModule,
    TranslocoDirective,
    GenericFormComponent
  ],
  templateUrl: './edit-subscription-modal.component.html',
  styleUrl: './edit-subscription-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditSubscriptionModalComponent implements OnInit {

  private readonly toastService = inject(ToastService);
  private readonly subscriptionService = inject(SubscriptionService);
  private readonly modal = inject(NgbActiveModal);
  private readonly genericFormFactoryService = inject(GenericFormFactoryService);

  subscription = model.required<Subscription>();
  providers = model.required<Provider[]>();
  metadata = model.required<FormControlDefinition[]>();

  formDefinition = signal<FormDefinition | undefined>(undefined);
  optionsFormDefinition = computed(() => {
    const form = this.formDefinition();
    if (!form) return null;

    return {
      key: form.key,
      descriptionKey: '',
      controls: this.metadata().filter(d => !d.advanced),
    }
  });
  advancedFormDefinition = computed(() => {
    const form = this.formDefinition();
    if (!form) return null;

    return {
      key: form.key,
      descriptionKey: '',
      controls: this.metadata().filter(d => d.advanced),
    }
  });

  activeTab: 'general' | 'options' | 'advanced' = 'general';

  subscriptionForm = new FormGroup({});

  ngOnInit(): void {
    this.subscriptionService.getForm().pipe(
      tap(form => this.formDefinition.set(form)),
    ).subscribe();
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
