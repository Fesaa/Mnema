export type Page = {
  id: string;
  sortValue: number;
  title: string;
  icon: string;
  provider: Provider;
  modifiers?: Modifier[];
  metadata?: DownloadMetadata,
  dirs: string[];
  customRootDir: string;
}

export type Modifier = {
  title: string;
  key: string;
  type: ModifierType;
  values: ModifierValue[];
}

export type ModifierValue = {
  key: string;
  value: string;
  default: boolean;
}

export enum Provider {
  NYAA = 0,
  MANGADEX = 1,
  WEBTOON = 2,
  DYNASTY = 3,
  BATO = 4,
  MANGABUDDY = 5
}

export const Providers = [
  {
    label: "Nyaa",
    value: Provider.NYAA
  },
  {
    label: "MangaDex",
    value: Provider.MANGADEX
  },
  {
    label: "Webtoon",
    value: Provider.WEBTOON
  },
  {
    label: "Dynasty",
    value: Provider.DYNASTY
  },
  {
    label: "Bato",
    value: Provider.BATO
  },
  {
    label:"Manga buddy",
    value: Provider.MANGABUDDY
  }
];


export const AllProviders = Object.values(Provider).filter(value => typeof value === 'number') as number[];

export enum ModifierType {
  DROPDOWN = 1,
  MULTI,
  SWITCH
}

export const AllModifierTypes = [ModifierType.DROPDOWN, ModifierType.MULTI, ModifierType.SWITCH]

export type DownloadMetadata = {
  definitions: DownloadMetadataDefinition[];
}

export type DownloadMetadataDefinition = {
  key: string;
  advanced: boolean;
  formType: DownloadMetadataFormType;
  defaultOption: string;
  options: MetadataOption[];
}

export type MetadataOption = {
  key: string;
  value: string;
}

export enum DownloadMetadataFormType {
  SWITCH,
  DROPDOWN,
  MULTI,
  TEXT
}
