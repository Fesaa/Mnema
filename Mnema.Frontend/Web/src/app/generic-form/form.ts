
export type FormDefinition = {
  key: string;
  descriptionKey: string;
  controls: FormControlDefinition[];
}
export type FormControlDefinition = {
  key: string;
  field: string;
  validators: { [key: string]: string[] };
  advanced: boolean;
  forceSingle: boolean;

  fieldType: FormType;
  valueType: ValueType;
  disabled: boolean;

  defaultValue: any;
  options?: FormControlOption[];
  controls?: FormControlDefinition[];
}
export type FormControlOption = {
  key: string;
  value: any;
  default: boolean;
}

export enum FormType {
  Switch,
  DropDown,
  MultiSelect,
  Text,
  Directory,
  MultiText,
  Array,
}

export enum ValueType {
  Boolean = 1,
  Integer = 2,
  String = 3,
}

