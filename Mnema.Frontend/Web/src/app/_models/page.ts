export type Page = {
  id: number;
  sortValue: number;
  title: string;
  icon: string;
  providers: Provider[];
  modifiers: Modifier[];
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
  NYAA = 2,
  YTS,
  LIMETORRENTS,
  SUBSPLEASE,
  MANGADEX,
  WEBTOON,
  DYNASTY,
  BATO,
  MANGABUDDY
}

export const Providers = [
  {
    label: "Nyaa",
    value: Provider.NYAA
  },
  {
    label: "YTS",
    value: Provider.YTS
  },
  {
    label: "LimeTorrents",
    value: Provider.LIMETORRENTS
  },
  {
    label: "SubsPlease",
    value: Provider.SUBSPLEASE
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
