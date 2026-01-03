import {FormControlDefinition} from "../generic-form/form";

export type Page = {
  id: string;
  sortValue: number;
  title: string;
  icon: string;
  provider: Provider;
  modifiers?: FormControlDefinition[];
  metadata?: DownloadMetadata,
  customRootDir: string;
}

export enum Provider {
  NYAA = 0,
  MANGADEX = 1,
  WEBTOON = 2,
  DYNASTY = 3,
  BATO = 4,
  MANGABUDDY = 5
}

export const AllProviders = Object.values(Provider).filter(value => typeof value === 'number') as number[];

export type DownloadMetadata = {
  definitions: FormControlDefinition[];
}

