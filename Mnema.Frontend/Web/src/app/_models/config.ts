import {MetadataProvider} from "@mnema/features/monitored-series/metadata.service";

export type Config = {
  maxConcurrentTorrents: number;
  maxConcurrentImages: number;
  subscriptionRefreshHour: number;
  version: string;
  firstInstalledVersion: string;
  installDate: Date;
  lastUpdateDate: Date;
  metadataProviderSettings: Record<keyof typeof MetadataProvider, MetadataProviderSettingsDto>;
}

export interface MetadataProviderSettingsDto {
  priority: number;
  enabled: boolean;
  seriesSettings: SeriesMetadataSettingsDto;
}

export interface SeriesMetadataSettingsDto {
  title: boolean;
  summary: boolean;
  localizedSeries: boolean;
  coverUrl: boolean;
  publicationStatus: boolean;
  year: boolean;
  ageRating: boolean;
  tags: boolean;
  people: boolean;
  links: boolean;
  chapters: boolean;
  chapterSettings: ChapterMetadataSettingsDto;
}

export interface ChapterMetadataSettingsDto {
  title: boolean;
  summary: boolean;
  cover: boolean;
  releaseDate: boolean;
  people: boolean;
}
