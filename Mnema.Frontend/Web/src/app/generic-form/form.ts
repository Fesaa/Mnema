
export type FormDefinition = {
  key: string;
  descriptionKey: string;
  controls: FormControlDefinition[];
}
export type FormControlDefinition = {
  key: string;
  translationPrefix: string;
  field: string;
  validators: { [key: string]: string[] };
  advanced: boolean;
  forceSingle: boolean;

  fieldType: FormType;
  valueType: ValueType;
  disabled: boolean;
  forceEditMode: boolean;
  inline: boolean;
  hideText: boolean;
  wikiLink?: string;
  hidden: boolean;

  defaultValue: any;
  options?: FormControlOption[];
  controls?: FormControlDefinition[];
}
export type FormControlOption = {
  key: string;
  translationPrefix?: string;
  value: any;
  default: boolean;
}

export enum FormType {
  Switch = 0,
  DropDown = 1,
  MultiSelect = 2,
  Text = 3,
  Directory = 4,
  MultiText = 5,
  Array = 6,
  CommaSeparatedValues = 7,
  CheckBox = 8,
  FieldRow = 9,
}

export enum ValueType {
  Boolean = 1,
  Integer = 2,
  String = 3,
}

