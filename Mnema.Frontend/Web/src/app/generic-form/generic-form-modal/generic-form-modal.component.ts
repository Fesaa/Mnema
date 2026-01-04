import {ChangeDetectionStrategy, Component, inject, input, model, signal} from '@angular/core';
import {DefaultValuePipe} from "../../_pipes/default-value.pipe";
import {ProviderNamePipe} from "../../_pipes/provider-name.pipe";
import {FormGroup, ReactiveFormsModule} from "@angular/forms";
import {SafeHtmlPipe} from "../../_pipes/safe-html-pipe";
import {SettingsItemComponent} from "../../shared/form/settings-item/settings-item.component";
import {TranslocoDirective} from "@jsverse/transloco";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {GenericFormComponent} from "../generic-form.component";
import {FormDefinition} from "../form";
import {GenericFormFactoryService} from "../generic-form-factory.service";

@Component({
  selector: 'app-generic-form-modal',
  imports: [
    ReactiveFormsModule,
    TranslocoDirective,
    GenericFormComponent
  ],
  templateUrl: './generic-form-modal.component.html',
  styleUrl: './generic-form-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GenericFormModalComponent<T> {

  private readonly modal = inject(NgbActiveModal);
  private readonly genericFormFactoryService = inject(GenericFormFactoryService)

  translationKey = model.required<string>();
  formDefinition = model.required<FormDefinition>();
  nullable = model(false);
  initialValue = model.required<T>();
  double = model.required<boolean>();

  formGroup = signal<FormGroup | null>(null);

  close() {
    this.modal.dismiss();
  }

  save() {
    this.modal.close({
      ...this.initialValue(),
      ...this.genericFormFactoryService.adjustForGenericMetadata(this.formGroup()?.value)
    });
  }
}
