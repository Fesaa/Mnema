import {ChangeDetectionStrategy, Component, inject, model, signal, TemplateRef} from '@angular/core';
import {FormGroup, ReactiveFormsModule} from "@angular/forms";
import {TranslocoDirective} from "@jsverse/transloco";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {GenericFormComponent} from "../generic-form.component";
import {FormDefinition} from "../form";
import {GenericFormFactoryService} from "../generic-form-factory.service";
import {NgTemplateOutlet} from "@angular/common";

@Component({
  selector: 'app-generic-form-modal',
  imports: [
    ReactiveFormsModule,
    TranslocoDirective,
    GenericFormComponent,
    NgTemplateOutlet
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
  double = model<boolean>(true);
  title = model<string>();
  descriptionTemplate = model<TemplateRef<any>>();

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
