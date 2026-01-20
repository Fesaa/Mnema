import {ComicInfoAgeRating} from "../../../_models/preferences";

export enum PublicationStatus {
  Ongoing = 0,
  Completed = 1,
  Paused = 2,
  Cancelled = 3,
  Unknown = 4,
}

export interface Series {
  id: string;
  title: string;
  localizedSeries?: string | null;
  summary: string;

  coverUrl?: string | null;
  refUrl?: string | null;

  status: PublicationStatus;
  translationStatus?: PublicationStatus | null;

  year?: number | null;

  highestVolumeNumber?: number | null;
  highestChapterNumber?: number | null;

  ageRating?: ComicInfoAgeRating | null;
  tags: Tag[];
  people: Person[];
  links: string[];

  chapters: Chapter[];
}

export interface Chapter {
  id: string;
  title: string;
  summary: string;

  volumeMarker: string;
  chapterMarker: string;

  coverUrl?: string | null;
  refUrl?: string | null;

  releaseDate?: string | null; // ISO string recommended for transport
  tags: Tag[];
  people: Person[];

  translationGroups: string[];
}

export interface Tag {
  id: string;
  value: string;
  isMarkedAsGenre: boolean;
}

export interface Person {
  name: string;
  roles: PersonRole[];
}

export enum PersonRole
{
  Writer = 0,
  Penciller = 1,
  Inker = 2,
  Colorist = 3,
  Letterer = 4,
  CoverArtist = 5,
  Editor = 6,
  Translator = 7,
  Publisher = 8,
  Imprint = 9,
  Character = 10,
}
