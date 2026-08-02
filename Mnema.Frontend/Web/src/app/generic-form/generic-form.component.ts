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
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {SettingsItemComponent} from "../shared/form/settings-item/settings-item.component";
import {DefaultValuePipe} from "../_pipes/default-value.pipe";
import {form} from "@angular/forms/signals";
import {TypeaheadComponent} from "../type-ahead/typeahead.component";
import {ModalService} from "../_services/modal.service";
import {filter, tap} from "rxjs";
import {
  SettingMultiTextFieldComponent
} from "@mnema/shared/form/setting-multi-text-field/setting-multi-text-field.component";
import {Breakpoint, UtilityService} from "@mnema/_services/utility.service";

@Component({
  selector: 'app-generic-form',
  imports: [
    ReactiveFormsModule,
    SettingsSwitchComponent,
    TranslocoDirective,
    SettingsItemComponent,
    DefaultValuePipe,
    TypeaheadComponent,
    SettingMultiTextFieldComponent,
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
  protected readonly utilityService = inject(UtilityService);

  formDefinition = input.required<FormDefinition>();
  initialValue = input.required<T>();
  nullable = input(false);
  double = input<boolean>(true);
  inline = input<boolean>(false);
  inModal = input<boolean>(false);
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

  getFormOptionsTranslation(control: FormControlDefinition, option: FormControlOption) {
    if (option.translationPrefix) {
      return translate(`${option.translationPrefix}.${option.key}`);
    }

    return translate(`${this.formDefinition().key}.${control.key}.${option.key}`);
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

  protected readonly Breakpoint = Breakpoint;

  private readonly collapseThreshold = 10;
  private collapsedArrays = new Set<string>();
  private hasRunInitialCollapse = new Set<string>();

  isArrayCollapsed(control: FormControlDefinition): boolean {
    if (!this.hasRunInitialCollapse.has(control.key)) {
      this.hasRunInitialCollapse.add(control.key);

      if (this.getFormArray(control).length > this.collapseThreshold) {
        this.collapsedArrays.add(control.key);
      }
    }

    return this.collapsedArrays.has(control.key);
  }

  toggleArrayCollapsed(control: FormControlDefinition): void {
    this.collapsedArrays.has(control.key)
      ? this.collapsedArrays.delete(control.key)
      : this.collapsedArrays.add(control.key);
  }

}
