import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input, output,
  Signal
} from '@angular/core';

import {FormControlDefinition, FormControlOption, FormDefinition, FormType} from "./form";
import {FormBuilder, FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule} from "@angular/forms";
import {GENERIC_METADATA_FIELD, GenericFormFactoryService} from "./generic-form-factory.service";
import {SettingsSwitchComponent} from "../shared/form/settings-switch/settings-switch.component";
import {TranslocoDirective} from "@jsverse/transloco";
import {SettingsItemComponent} from "../shared/form/settings-item/settings-item.component";
import {DefaultValuePipe} from "../_pipes/default-value.pipe";
import {form} from "@angular/forms/signals";
import {TypeaheadComponent} from "../type-ahead/typeahead.component";
import {ModalService} from "../_services/modal.service";
import {filter, tap} from "rxjs";

@Component({
  selector: 'app-generic-form',
  imports: [
    ReactiveFormsModule,
    SettingsSwitchComponent,
    TranslocoDirective,
    SettingsItemComponent,
    DefaultValuePipe,
    TypeaheadComponent,
  ],
  templateUrl: './generic-form.component.html',
  styleUrl: './generic-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GenericFormComponent<T> {

  private readonly nonNullableFormGroupBuilder = inject(NonNullableFormBuilder);
  private readonly nullableFormGroupBuilder = inject(FormBuilder);
  protected readonly genericFormFactoryService = inject(GenericFormFactoryService);
  private readonly modalService = inject(ModalService);

  formDefinition = input.required<FormDefinition>();
  initialValue = input.required<T>();
  nullable = input(false);
  double = input<boolean>(true);
  supplyFormGroup = input<FormGroup>();

  formGroupTracker = output<FormGroup>();

  protected formGroupBuilder = computed(() =>
    this.nullable() ? this.nullableFormGroupBuilder : this.nonNullableFormGroupBuilder);

  protected genericMetadataFieldPresent = computed(() => this.formDefinition().controls
    .some(control => control.field === GENERIC_METADATA_FIELD));

  genericForm: Signal<FormGroup> = computed(() => {
    const formDefinition = this.formDefinition();
    const fb = this.formGroupBuilder();

    // Type safety is only for the consumer, we cannot make such guarantees
    const obj = this.initialValue() as any;

    const formGroup: FormGroup = this.supplyFormGroup() ?? fb.group({})
    for (let control of formDefinition.controls) {
      if (control.field === GENERIC_METADATA_FIELD)
        continue;

      if (formGroup.get(control.field)) {
        console.warn(`The FormGroup already included a control for ${control.field}, skipping`);
        continue;
      }

      const formControl = fb.control(
        this.genericFormFactoryService.initialValue(obj, control),
        this.genericFormFactoryService.validators(control.validators),
        );

      formGroup.addControl(control.field, formControl);
    }

    const genericMetadataGroup = formGroup.get(GENERIC_METADATA_FIELD) as FormGroup | undefined;
    if (this.genericMetadataFieldPresent()) {
      const metadataBag = obj[GENERIC_METADATA_FIELD];
      const controls = formDefinition.controls
        .filter(control => control.field === GENERIC_METADATA_FIELD);

      const control = this.genericFormFactoryService.genericMetadataGroup(
        metadataBag,
        controls,
        fb,
        genericMetadataGroup,
      );

      if (!!genericMetadataGroup) {
        formGroup.setControl(GENERIC_METADATA_FIELD, control);
      } else {
        formGroup.addControl(GENERIC_METADATA_FIELD, control);
      }

    }

    this.formGroupTracker.emit(formGroup);

    return formGroup;
  });

  protected getFormControl(control: FormControlDefinition): FormControl {
    if (control.field === GENERIC_METADATA_FIELD) {
      return this.genericForm().get(GENERIC_METADATA_FIELD)?.get(control.key) as FormControl;
    }

    return this.genericForm().get(control.field) as FormControl;
  }

  protected getFormGroup(control: FormControlDefinition) {
    if (control.field === GENERIC_METADATA_FIELD) {
      return this.genericForm().get(GENERIC_METADATA_FIELD) as FormGroup;
    }

    return this.genericForm();
  }

  protected getFormControlName(control: FormControlDefinition) {
    if (control.field === GENERIC_METADATA_FIELD) {
      return control.key;
    }

    return control.field;
  }

  protected getFormOption(control: FormControlDefinition, value: any) {
    return control.options.find(option => option.value === value);
  }

  protected patchTypeAheadControlValue($event: FormControlOption[] | FormControlOption, formControl: FormControl) {
    const options = Array.isArray($event) ? $event : [$event];
    const formValue = options.map(option => option.value);

    formControl.setValue(formValue);
  }

  protected readonly FormType = FormType;
  protected readonly form = form;

  protected pickDirectory(formControl: FormControl) {
    this.modalService.getDirectory$('', {copy: true, filter: true, create: true, showFiles: false}).pipe(
      filter(directory => !!directory),
      tap(directory => formControl.setValue(directory)),
    ).subscribe();
  }
}
