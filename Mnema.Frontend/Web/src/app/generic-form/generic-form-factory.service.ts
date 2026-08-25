import {inject, Injectable} from '@angular/core';
import {
  AsyncValidatorFn,
  FormArray,
  FormBuilder,
  FormGroup,
  NonNullableFormBuilder,
  ValidatorFn,
  Validators
} from "@angular/forms";
import {FormControlDefinition, FormControlOption, FormType, ValueType} from "./form";
import {TypeaheadSettings} from "../type-ahead/typeahead.component";
import {of} from "rxjs";
import {MnemaValidators} from "../shared/validators";
import {LoggingService, LogLevel} from "@mnema/_services/logging-service";
import {HttpClient} from "@angular/common/http";

export type GenericBag = { [key: string]: any[] };

export const GENERIC_METADATA_FIELD = "metadata";

@Injectable({
  providedIn: 'root',
})
export class GenericFormFactoryService extends LoggingService {

  private readonly httpClient = inject(HttpClient);

  override name: string = 'GenericFormFactoryService';
  override logLevel: LogLevel = LogLevel.INFO;

  createArrayItem(
    control: FormControlDefinition,
    fb: FormBuilder | NonNullableFormBuilder
  ): FormGroup {
    this.log(`createArrayItem called for control '${control.field}'`, { control });
    return this.createFormGroup({}, control.controls ?? [], fb);
  }

  createFormArray(
    values: any[],
    control: FormControlDefinition,
    fb: FormBuilder | NonNullableFormBuilder
  ): FormArray<FormGroup> {
    this.log(`createFormArray called for field '${control.field}'`, { values, control });

    const array = fb.array<FormGroup>([]);

    for (const value of values ?? []) {
      this.log(`Pushing array item into '${control.field}'`, { value });
      array.push(
        this.createFormGroup(
          value,
          control.controls ?? [],
          fb
        )
      );
    }

    this.log(`FormArray created for '${control.field}' with ${array.length} items.`);
    return array;
  }

  createFormGroup(
    obj: any,
    controls: FormControlDefinition[],
    fb: FormBuilder | NonNullableFormBuilder,
    existing?: FormGroup
  ): FormGroup {
    this.log(`createFormGroup called`, { obj, controlsCount: controls?.length, existingProvided: !!existing });

    const formGroup: FormGroup = existing ?? fb.group({});

    controls = controls.flatMap(c => c.fieldType === FormType.FieldRow ? c.controls! : [c]);

    for (const control of controls) {

      if (control.field === GENERIC_METADATA_FIELD) {
        this.log(`Skipping metadata control in main iteration for field '${control.field}'`);
        continue;
      }

      if (formGroup.contains(control.field)) {
        this.log(`FormGroup already contains control '${control.field}', skipping.`);
        continue;
      }

      if (control.fieldType === FormType.Array) {
        const rawValues = obj?.[control.field] ?? [];
        this.log(`Adding FormArray control for field '${control.field}'`, { rawValues });
        formGroup.addControl(
          control.field,
          this.createFormArray(
            rawValues,
            control,
            fb
          )
        );
      } else {
        const initVal = this.initialValue(obj, control);
        const [validators, asyncValidators] = this.validators(control.validators);
        this.log(`Adding FormControl for field '${control.field}'`, { initialValue: initVal });

        formGroup.addControl(
          control.field,
          fb.control(
            initVal,
            validators,
            asyncValidators,
          )
        );
      }
    }

    const metadataControls = controls.filter(c => c.field === GENERIC_METADATA_FIELD);

    if (metadataControls.length) {
      this.log(`Processing ${metadataControls.length} metadata control(s)`);
      const existingMetadata = formGroup.get(GENERIC_METADATA_FIELD) as FormGroup | null;

      const metadataGroup = this.genericMetadataGroup(
        obj?.[GENERIC_METADATA_FIELD] ?? {},
        metadataControls,
        fb,
        existingMetadata ?? undefined
      );

      if (existingMetadata) {
        this.log(`Updating existing metadata FormGroup`);
        formGroup.setControl(GENERIC_METADATA_FIELD, metadataGroup);
      } else {
        this.log(`Adding new metadata FormGroup`);
        formGroup.addControl(GENERIC_METADATA_FIELD, metadataGroup);
      }
    }

    return formGroup;
  }

  createTypeAheadSettings(obj: any, control: FormControlDefinition, inModal: boolean): TypeaheadSettings<FormControlOption> {
    this.log(`createTypeAheadSettings called for control '${control.key}'`, { obj, controlType: control.fieldType });

    if (control.fieldType !== FormType.MultiSelect && control.fieldType !== FormType.MultiText) {
      this.log(`Invalid control type for TypeAhead: ${control.fieldType}`);
      throw new Error(`Invalid control type for ${control.fieldType}`);
    }

    const settings = new TypeaheadSettings<FormControlOption>();
    settings.id = control.key;
    settings.multiple = true;
    settings.minCharacters = 0;
    settings.dropdownPosition = inModal ? 'body' : 'relative';

    if (control.fieldType === FormType.MultiText) {
      settings.addIfNonExisting = true;
      settings.addTransformFn = (text) => ({key: text, value: text, default: false});
      settings.compareFnForAdd = (optionList, filter) =>
        optionList.filter(v => (v.value + '').toLowerCase().includes(filter.toLowerCase()));
    }

    settings.fetchFn = (f) => {
      this.log(`TypeAhead fetchFn called with filter: '${f}' for key '${control.key}'`);
      const filtered = (control.options ?? [])
        .filter(v => (v.value + '').toLowerCase().includes(f.toLowerCase()));

      return of(filtered);
    };

    if (obj) {
      const array = Array.isArray(obj) ? obj : [obj];
      settings.savedData = array.map(v =>
        control.options?.find(o => o.value == v) ?? (control.fieldType === FormType.MultiText ? {
          key: v + '',
          value: v,
          default: false
        } : undefined))
        .filter(v => !!v) as FormControlOption[];
      this.log(`TypeAhead initial savedData derived`, { savedData: settings.savedData });
    } else {
      settings.savedData = [];
    }

    settings.trackByIdentityFn = (idx, option) => `${option.key}`;
    settings.selectionCompareFn = (option1, option2) => option1.key === option2.key;

    return settings;
  }

  adjustForGenericMetadata(obj?: any) {
    this.log(`adjustForGenericMetadata input`, { obj });

    if (!obj) return obj;

    if (!Object.hasOwn(obj, GENERIC_METADATA_FIELD)) {
      this.log(`No '${GENERIC_METADATA_FIELD}' field present in object.`);
      return obj;
    }

    for (let key in obj[GENERIC_METADATA_FIELD]) {
      const val = obj[GENERIC_METADATA_FIELD][key];

      if (val === null || val === undefined) {
        obj[GENERIC_METADATA_FIELD][key] = [];
      } else if (Array.isArray(val)) {
        obj[GENERIC_METADATA_FIELD][key] = val
          .filter(v => v !== null && v !== undefined)
          .map(v => this.serializeMetadataValue(v));
      } else {
        obj[GENERIC_METADATA_FIELD][key] = [this.serializeMetadataValue(val)];
      }
    }

    this.log(`adjustForGenericMetadata output`, { obj });
    return obj;
  }

  /**
   * Extends an existing FormGroup to ensure it has enough controls (e.g., FormArray elements)
   * to accommodate the structure of the incoming value object.
   * Does NOT set or update the actual form values.
   *
   * @param formGroup The FormGroup to extend
   * @param controls The array of control definitions for this group
   * @param value The target value object providing the required array lengths/structures
   * @param fb The FormBuilder instance to use when instantiating new controls
   */
  extendFormGroupForValue(
    formGroup: FormGroup,
    controls: FormControlDefinition[],
    value: any,
    fb: FormBuilder | NonNullableFormBuilder
  ): void {
    this.log(`extendFormGroupForValue called`, { controlsCount: controls?.length, value });

    if (!formGroup || !controls || !value) {
      return;
    }

    const flattenedControls = controls.flatMap(c =>
      c.fieldType === FormType.FieldRow ? (c.controls ?? []) : [c]
    );

    for (const controlDef of flattenedControls) {
      if (controlDef.field === GENERIC_METADATA_FIELD) {
        throw new Error(`FormGroups with ${GENERIC_METADATA_FIELD} cannot be extended`);
      }

      if (controlDef.fieldType === FormType.Array) {
        const arrayControl = formGroup.get(controlDef.field) as FormArray<FormGroup> | null;
        const targetArrayValue: any[] = value?.[controlDef.field] ?? [];

        if (arrayControl && Array.isArray(targetArrayValue)) {
          const currentLength = arrayControl.length;
          const requiredLength = targetArrayValue.length;

          if (requiredLength > currentLength) {
            this.log(`Extending FormArray '${controlDef.field}' from ${currentLength} to ${requiredLength} items.`);

            for (let i = currentLength; i < requiredLength; i++) {
              const newItem = this.createArrayItem(controlDef, fb);
              arrayControl.push(newItem);
            }
          }

          for (let i = 0; i < requiredLength; i++) {
            const childGroup = arrayControl.at(i) as FormGroup;
            const childValue = targetArrayValue[i];

            if (childGroup && childValue && controlDef.controls) {
              this.extendFormGroupForValue(childGroup, controlDef.controls, childValue, fb);
            }
          }
        }
      }
    }
  }

  private serializeMetadataValue(v: any): string {
    if (v !== null && typeof v === 'object') {
      const serialized = JSON.stringify(v);
      this.log(`Serialized object metadata value`, { original: v, serialized });
      return serialized;
    }
    return v + '';
  }

  private genericMetadataGroup(
    metadata: GenericBag,
    controls: FormControlDefinition[],
    fb: FormBuilder | NonNullableFormBuilder,
    formGroup?: FormGroup
  ): FormGroup {
    this.log(`genericMetadataGroup called`, { metadata, controlsCount: controls?.length });
    const group = formGroup ?? fb.group({});

    for (let control of controls) {

      if (control.fieldType === FormType.Array) {
        const rawValues = metadata[control.key];

        let sourceValues = (rawValues && rawValues.length > 0) ? rawValues : control.defaultValue;
        if (!Array.isArray(sourceValues)) {
          this.log(`Warning: sourceValues for '${control.key}' is not an array. Falling back to [].`, { sourceValues });
          sourceValues = [];
        }

        this.log(`Creating metadata FormArray for key '${control.key}'`, { sourceValues });

        const formArray = this.createFormArray(
          sourceValues.map((v: any) => this.parseMetadataArrayItem(v)),
          control,
          fb
        );

        group.addControl(control.key, formArray);
        continue;
      }

      const currentValues = metadata[control.key];
      const initialValue = currentValues && currentValues.length > 0 ? currentValues : control.defaultValue;
      const transformedValue = this.transFormValueForFormType(initialValue, control);

      this.log(`Creating metadata FormControl for key '${control.key}'`, { initialValue, transformedValue });

      const [validators, asyncValidators] = this.validators(control.validators);
      const formControl = fb.control(
        transformedValue,
        validators,
        asyncValidators,
      );

      group.addControl(control.key, formControl);
    }

    return group;
  }

  private parseMetadataArrayItem(value: any): any {
    if (typeof value !== 'string') {
      return value;
    }

    try {
      const parsed = JSON.parse(value);
      this.log(`Successfully parsed metadata array JSON item`, { value, parsed });
      return parsed;
    } catch (e) {
      console.warn(`GenericFormFactoryService: failed to parse metadata array item as JSON, falling back to {}. Value: ${value}`, e);
      return {};
    }
  }

  private validators(data: GenericBag): [ValidatorFn[], AsyncValidatorFn[]]{
    const validators: ValidatorFn[] = [];
    const asyncValidators: AsyncValidatorFn[] = [];

    for (let key in data) {
      const args = data[key];
      const [validator, isAsync] = this.validator(key, args);
      if (!validator) {
        this.warn(`No validator found matching key '${key}'`);
        continue;
      }

      if (isAsync) {
        asyncValidators.push(validator);
      } else {
        validators.push(validator);
      }
    }

    this.log(`Built ${validators.length} validator(s)`, { data });
    return [validators, asyncValidators];
  }

  private validator(key: string, args: any[]): [ValidatorFn | null, false] | [AsyncValidatorFn | null, true] {
    this.log(`Evaluating validator '${key}'`, { args });
    switch (key) {
      case "required":
        return [Validators.required, false];
      case "minLength":
        return [Validators.minLength(args[0]), false];
      case "maxLength":
        return [Validators.maxLength(args[0]), false];
      case "min":
        return [Validators.min(args[0]), false];
      case "max":
        return [Validators.max(args[0]), false];
      case "pattern":
        return [Validators.pattern(args[0]), false];
      case "startsWith":
        return [MnemaValidators.startsWith(args[0]), false];
      case 'isUrl':
        return [MnemaValidators.isUrl, false];
      case 'serverSideValidation':
        return [MnemaValidators.serverSideValidation(this.httpClient, args[0]), true];
    }

    return [null, false];
  }

  private initialValue(obj: any, control: FormControlDefinition) {
    let fieldName = control.field;

    if (control.field === GENERIC_METADATA_FIELD) {
      obj = obj[GENERIC_METADATA_FIELD];
      fieldName = control.key;
    }

    const value = this.getNestedValue(obj, fieldName, control.defaultValue);
    const transformed = this.transFormValueForFormType(value, control);

    this.log(`Derived initial value for '${control.field}' (key: '${control.key}')`, { raw: value, transformed });
    return transformed;
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
    switch (control.fieldType) {
      case FormType.Switch:
      case FormType.CheckBox:
        return this.transFormValue(value, ValueType.Boolean);
      case FormType.DropDown:
        return this.transFormValue(value, control.valueType);
      case FormType.MultiSelect:
      case FormType.MultiText:
      case FormType.CommaSeparatedValues:
        if (value === '' || value === null || value === undefined) {
          return [];
        }
        return Array.isArray(value) ? value.map(v => this.transFormValue(v, control.valueType)) : [this.transFormValue(value, control.valueType)];
      case FormType.Text:
      case FormType.Directory:
        return this.transFormValue(value, ValueType.String);
      case FormType.FieldRow:
      case FormType.Array:
        this.log(`Error: FormTypes.Array passed into transFormValueForFormType`);
        throw new Error('Error FormTypes should be handled separately');
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
