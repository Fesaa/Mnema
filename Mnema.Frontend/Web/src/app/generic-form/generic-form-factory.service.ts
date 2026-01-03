import {Injectable} from '@angular/core';
import {FormBuilder, FormGroup, NonNullableFormBuilder, ValidatorFn, Validators} from "@angular/forms";
import {FormControlDefinition, FormControlOption, FormPipe, FormType, ValueType} from "./form";
import {TypeaheadSettings} from "../type-ahead/typeahead.component";
import {of} from "rxjs";
export type GenericBag = { [key: string]: any[] };

export const GENERIC_METADATA_FIELD = "metadata";

@Injectable({
  providedIn: 'root',
})
export class GenericFormFactoryService {

  createTypeAheadSettings(obj: any, control: FormControlDefinition): TypeaheadSettings<FormControlOption> {
    if (control.type !== FormType.MULTI)
      throw new Error(`Invalid control type for ${control.type}`);

    const settings = new TypeaheadSettings<FormControlOption>();
    settings.id = control.key
    settings.multiple = true;
    settings.minCharacters = 0;

    settings.fetchFn = (f) => {
      const filtered = control.options
        .filter(v => (v.value + '').toLowerCase().includes(f.toLowerCase()));

      return of(filtered);
    }

    if (obj) {
      settings.savedData = (obj as Array<any>).map(v =>
        control.options.find(o => o.value == v))
        .filter(v => !!v);
    } else {
      settings.savedData = [];
    }

    settings.trackByIdentityFn = (idx, option) => `${option.key}`;
    settings.selectionCompareFn = (option1, option2) => option1.key === option2.key;

    return settings;
  }

  adjustForGenericMetadata(obj?: any) {
    if (!obj) return obj;

    if (!Object.hasOwn(obj, GENERIC_METADATA_FIELD)) {
      return obj;
    }

    for (let key in obj[GENERIC_METADATA_FIELD]) {
      const val = obj[GENERIC_METADATA_FIELD][key];
      obj[GENERIC_METADATA_FIELD][key] = Array.isArray(val) ? val.map(v => v + '') : [val + ''];
    }

    return obj;
  }

  genericMetadataGroup(metadata: GenericBag, controls: FormControlDefinition[], fb: FormBuilder | NonNullableFormBuilder): FormGroup {
    const group = fb.group({});

    for (let control of controls) {
      const currentValues = metadata[control.key];
      const initialValue = currentValues && currentValues.length > 0 ? currentValues : control.defaultOption;

      const controlValue = control.type === FormType.MULTI
        ? Array.isArray(initialValue) ? initialValue.map(v => this.transFormValue(v, control.valueType)) : [this.transFormValue(initialValue, control.valueType)]
        : this.transFormValue(initialValue[0] ?? '', control.valueType);

      const formControl = fb.control(controlValue, this.validators(control.validators));

      group.addControl(control.key, formControl);
    }



    return group;
  }

  validators(data: GenericBag): ValidatorFn[] {
    const validators: ValidatorFn[] = [];

    for (let key in data) {
      const args = data[key];

      const validator = this.validator(key, args);
      if (validator) {
        validators.push(validator);
      }
    }

    return validators;
  }

  validator(key: string, args: any[]): ValidatorFn | null {
    console.log("Creating validator", key, args);
    switch (key) {
      case "required":
        return Validators.required;
      case "minLength":
        return Validators.minLength(args[0]);
      case "maxLength":
        return Validators.maxLength(args[0]);
    }

    return null;
  }

  initialValue(obj: any, control: FormControlDefinition) {
    let fieldName = control.field;

    if (control.field === GENERIC_METADATA_FIELD) {
      obj = obj[GENERIC_METADATA_FIELD];
      fieldName = control.key;
    }

    const value = (obj && obj.hasOwnProperty(fieldName)) ? obj[fieldName] : control.defaultOption;

    switch (control.type) {
      case FormType.SWITCH:
        return this.transFormValue(value, ValueType.Boolean);
      case FormType.DROPDOWN:
        return this.transFormValue(value, control.valueType);
      case FormType.MULTI:
        return Array.isArray(value) ? value.map(v => this.transFormValue(v, control.valueType)) : [this.transFormValue(value, control.valueType)];
      case FormType.TEXT:
        return this.transFormValue(value, ValueType.String);
    }
  }

  private transFormValue(value: any, valueType: ValueType) {
    switch (valueType) {
      case ValueType.Boolean:
        return typeof value === 'boolean' ? value : (value + '').toLowerCase() === 'true';
      case ValueType.Integer:
        return typeof value === 'number' ? value : parseInt(value, 10);
      case ValueType.String:
        return typeof value === 'string' ? value : (value + '');
    }
  }

}
