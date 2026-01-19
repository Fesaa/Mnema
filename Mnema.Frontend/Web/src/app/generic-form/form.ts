
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

  type: FormType;
  valueType: ValueType;
  pipe?: FormPipe,
  disabled: boolean;

  defaultOption: any;
  options: FormControlOption[];
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
  MultiText
}

export enum ValueType {
  Boolean = 1,
  Integer = 2,
  String = 3,
}

export enum FormPipe {
  ExternalConnectionEvent = 0,
  ExternalConnectionType = 1,
}
