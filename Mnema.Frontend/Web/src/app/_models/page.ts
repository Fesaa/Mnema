import {FormControlDefinition} from "../generic-form/form";
import {MetadataBag} from "@mnema/_models/search";

export type Page = {
  id: string;
  sortValue: number;
  title: string;
  icon: string;
  provider: Provider;
  modifiers?: FormControlDefinition[];
  metadata?: FormControlDefinition[],
  customRootDir: string;
  defaultOptions: MetadataBag;
}

export enum Provider {
  NYAA = 0,
  MANGADEX = 1,
  WEBTOON = 2,
  DYNASTY = 3,
  BATO = 4,
  WEEBDEX = 5,
  COMIX = 6,
  KAGANE = 7,
  Madokami = 8,
  AthreaScans = 9,
}

export const InUseProviders: Provider[] = [
  Provider.NYAA,
  Provider.MANGADEX,
  Provider.WEBTOON,
  Provider.DYNASTY,
  Provider.Madokami,
  Provider.AthreaScans,
]
