export type Preferences = {
  imageFormat: ImageFormat;
  coverFallbackMethod: CoverFallbackMethod,
  convertToGenreList: string[],
  blackListedTags: string[],
  whiteListedTags: string[],
  ageRatingMappings: AgeRatingMap[],
  tagMappings: TagMap[],
};

export enum ImageFormat {
  Upstream = 0,
  Webp = 1,
}

export const ImageFormats = [
  {label: "Upstream", value: ImageFormat.Upstream},
  {label: "Webp", value: ImageFormat.Webp}
];

export enum CoverFallbackMethod {
  CoverFallbackFirst = 0,
  CoverFallbackLast = 1,
  CoverFallbackNone = 2,
}

export const CoverFallbackMethods = [
  {label: "First", value: CoverFallbackMethod.CoverFallbackFirst},
  {label: "Last", value: CoverFallbackMethod.CoverFallbackLast},
  {label: "None", value: CoverFallbackMethod.CoverFallbackNone},
]

export type TagMap = {
  originTag: string,
  destinationTag: string,
}

export type AgeRatingMap = {
  tag: string
  ageRating: ComicInfoAgeRating
}

export enum ComicInfoAgeRating {
  Unknown = 0,
  Pending = 1,
  EarlyChildhood = 2,
  Everyone = 3,
  G = 4,
  Everyone10Plus = 5,
  PG = 6,
  KidsToAdults = 7,
  Teen = 8,
  MA15Plus = 9,
  Mature17Plus = 10,
  M = 11,
  R18Plus = 12,
  AdultsOnly18Plus = 13,
  X18Plus = 14
}

export const ComicInfoAgeRatings = [
  {
    label: "Unknown",
    value: ComicInfoAgeRating.Unknown,
  },
  {
    label: "Rating Pending",
    value: ComicInfoAgeRating.Pending,
  },
  {
    label: "Early Childhood",
    value: ComicInfoAgeRating.EarlyChildhood,
  },
  {
    label: "Everyone",
    value: ComicInfoAgeRating.Everyone,
  },
  {
    label: "G",
    value: ComicInfoAgeRating.G,
  },
  {
    label: "Everyone 10+",
    value: ComicInfoAgeRating.Everyone10Plus,
  },
  {
    label: "PG",
    value: ComicInfoAgeRating.PG,
  },
  {
    label: "Kids to Adults",
    value: ComicInfoAgeRating.KidsToAdults,
  },
  {
    label: "Teen",
    value: ComicInfoAgeRating.Teen,
  },
  {
    label: "MA15+",
    value: ComicInfoAgeRating.MA15Plus,
  },
  {
    label: "Mature 17+",
    value: ComicInfoAgeRating.Mature17Plus,
  },
  {
    label: "M",
    value: ComicInfoAgeRating.M,
  },
  {
    label: "R18+",
    value: ComicInfoAgeRating.R18Plus,
  },
  {
    label: "Adults Only 18+",
    value: ComicInfoAgeRating.AdultsOnly18Plus,
  },
  {
    label: "X18+",
    value: ComicInfoAgeRating.X18Plus,
  }
];


