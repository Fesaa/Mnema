import {ChangeDetectionStrategy, Component, computed, inject, input, output, Signal, untracked} from '@angular/core';

import {FormControlDefinition, FormControlOption, FormDefinition, FormType} from "./form";
import {
  FormArray,
  FormBuilder,
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule
} from "@angular/forms";
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

  genericForm = computed(() => {

    const fb = this.formGroupBuilder();

    const formGroup = this.genericFormFactoryService.createFormGroup(
      untracked(this.initialValue),
      this.formDefinition().controls,
      fb,
      this.supplyFormGroup()
    );

    this.formGroupTracker.emit(formGroup);

    return formGroup;
  });

  protected getFormControl(control: FormControlDefinition): FormControl {
    if (control.field === GENERIC_METADATA_FIELD) {
      return this.genericForm().get(GENERIC_METADATA_FIELD)?.get(control.key) as FormControl;
    }

    return this.genericForm().get(control.field) as FormControl;
  }

  protected getFormArray(control: FormControlDefinition): FormArray<FormGroup> {
    if (control.field === GENERIC_METADATA_FIELD) {
      return this.genericForm().get(GENERIC_METADATA_FIELD)?.get(control.key) as FormArray<FormGroup>;
    }

    return this.genericForm().get(control.field) as FormArray<FormGroup>;
  }

  protected addArrayItem(control: FormControlDefinition): void {
    this.getFormArray(control).push(
      this.genericFormFactoryService.createArrayItem(
        control,
        this.formGroupBuilder()
      )
    );
  }

  protected removeArrayItem(control: FormControlDefinition, index: number) {
    this.getFormArray(control).removeAt(index);
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
    return control.options?.find(option => option.value === value);
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
