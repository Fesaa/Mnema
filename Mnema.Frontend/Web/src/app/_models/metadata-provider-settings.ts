import {MetadataBag} from "@mnema/_models/search";
import {MetadataProvider} from "@mnema/features/monitored-series/metadata.service";

export interface MetadataProviderSettings {
  id: string;
  metadataProvider: MetadataProvider;

  priority: number;

  enabled: boolean;

  seriesTitle: boolean;
  seriesSummary: boolean;
  seriesLocalizedName: boolean;
  seriesCoverUrl: boolean;
  seriesPublicationStatus: boolean;
  seriesAgeRating: boolean;
  seriesYear: boolean;
  seriesTags: boolean;
  seriesPeople: boolean;
  seriesLinks: boolean;

  chapters: boolean;
  chapterTitle: boolean;
  chapterSummary: boolean;
  chapterReleaseDate: boolean;
  chapterPeople: boolean;
  chapterTags: boolean;
  chapterCoverUrl: boolean;

  metadataProviderSpecific: MetadataBag;
}
