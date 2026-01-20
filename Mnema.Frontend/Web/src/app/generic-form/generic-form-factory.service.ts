import {Injectable} from '@angular/core';
import {FormBuilder, FormGroup, NonNullableFormBuilder, ValidatorFn, Validators} from "@angular/forms";
import {FormControlDefinition, FormControlOption, FormType, ValueType} from "./form";
import {TypeaheadSettings} from "../type-ahead/typeahead.component";
import {of} from "rxjs";
import {MnemaValidators} from "../shared/validators";

export type GenericBag = { [key: string]: any[] };

export const GENERIC_METADATA_FIELD = "metadata";

@Injectable({
  providedIn: 'root',
})
export class GenericFormFactoryService {

  createTypeAheadSettings(obj: any, control: FormControlDefinition): TypeaheadSettings<FormControlOption> {
    if (control.type !== FormType.MultiSelect && control.type !== FormType.MultiText)
      throw new Error(`Invalid control type for ${control.type}`);

    const settings = new TypeaheadSettings<FormControlOption>();
    settings.id = control.key
    settings.multiple = true;
    settings.minCharacters = 0;

    if (control.type === FormType.MultiText) {
      settings.addIfNonExisting = true;
      settings.addTransformFn = (text) => ({key: text, value: text, default: false});
      settings.compareFnForAdd = (optionList, filter) =>
        optionList.filter(v => (v.value + '').toLowerCase().includes(filter.toLowerCase()));
    }

    settings.fetchFn = (f) => {
      const filtered = control.options
        .filter(v => (v.value + '').toLowerCase().includes(f.toLowerCase()));

      return of(filtered);
    }

    if (obj) {
      const array = Array.isArray(obj) ? obj : [obj];
      settings.savedData = array.map(v =>
        control.options.find(o => o.value == v) ?? (control.type === FormType.MultiText ? {
          key: v + '',
          value: v,
          default: false
        } : undefined))
        .filter(v => !!v) as FormControlOption[];
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

      delete obj[key];
    }

    return obj;
  }

  adjustForNestedControls(obj: any | undefined, controls: FormControlDefinition[]) {
    obj = this.adjustForGenericMetadata(obj);

    const result: any = {};

    for (const control of controls) {
      const fieldName = control.field;
      const value = obj?.[fieldName];

      if (value === undefined) continue;

      const keys = fieldName.split('.');
      let current = result;

      for (let i = 0; i < keys.length - 1; i++) {
        const key = keys[i];
        if (!current[key]) {
          current[key] = {};
        }
        current = current[key];
      }

      current[keys[keys.length - 1]] = value;
    }

    return result;
  }

  genericMetadataGroup(
    metadata: GenericBag,
    controls: FormControlDefinition[],
    fb: FormBuilder | NonNullableFormBuilder,
    formGroup?: FormGroup
  ): FormGroup {
    const group = formGroup ?? fb.group({});

    for (let control of controls) {
      const currentValues = metadata[control.key];
      const initialValue = currentValues && currentValues.length > 0 ? currentValues : control.defaultOption;

      const formControl = fb.control(
        this.transFormValueForFormType(initialValue, control),
        this.validators(control.validators),
      );

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
    switch (key) {
      case "required":
        return Validators.required;
      case "minLength":
        return Validators.minLength(args[0]);
      case "maxLength":
        return Validators.maxLength(args[0]);
      case "min":
        return Validators.min(args[0]);
      case "max":
        return Validators.max(args[0]);
      case "pattern":
        return Validators.pattern(args[0]);
      case "startsWith":
        return MnemaValidators.startsWith(args[0]);
      case 'isUrl':
        return MnemaValidators.isUrl;
    }

    return null;
  }

  initialValue(obj: any, control: FormControlDefinition) {
    let fieldName = control.field;

    if (control.field === GENERIC_METADATA_FIELD) {
      obj = obj[GENERIC_METADATA_FIELD];
      fieldName = control.key;
    }

    const value = this.getNestedValue(obj, fieldName, control.defaultOption);

    return this.transFormValueForFormType(value, control);
  }

  private getNestedValue(obj: any, path: string, defaultValue: any): any {
    if (!obj) return defaultValue;

    const keys = path.split('.');
    let value = obj;

    for (const key of keys) {
      if (value && value.hasOwnProperty(key)) {
        value = value[key];
      } else {
        return defaultValue;
      }
    }

    return value;
  }

  private transFormValueForFormType(value: any, control: FormControlDefinition) {
    switch (control.type) {
      case FormType.Switch:
        return this.transFormValue(value, ValueType.Boolean);
      case FormType.DropDown:
        return this.transFormValue(value, control.valueType);
      case FormType.MultiSelect:
      case FormType.MultiText:
        return Array.isArray(value) ? value.map(v => this.transFormValue(v, control.valueType)) : [this.transFormValue(value, control.valueType)];
      case FormType.Text:
      case FormType.Directory:
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
