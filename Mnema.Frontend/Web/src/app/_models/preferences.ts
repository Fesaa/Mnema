export type Preferences = {
  subscriptionRefreshHour: number,
  logEmptyDownloads: boolean,
  logSubNoDownloads: boolean,
  convertToWebp: boolean,
  coverFallbackMethod: CoverFallbackMethod,
  genreList: string[],
  blackList: string[],
  whiteList: string[],
  ageRatingMappings: AgeRatingMap[],
  tagMappings: TagMap[],
};

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
  comicInfoAgeRating: ComicInfoAgeRating
}

export enum ComicInfoAgeRating {
  Unknown = "Unknown",
  Pending = "Rating Pending",
  EarlyChildhood = "Early Childhood",
  Everyone = "Everyone",
  G = "G",
  Everyone10Plus = "Everyone 10+",
  PG = "PG",
  KidsToAdults = "Kids to Adults",
  Teen = "Teen",
  MA15Plus = "MA15+",
  Mature17Plus = "Mature 17+",
  M = "M",
  R18Plus = "R18+",
  AdultsOnly18Plus = "Adults Only 18+",
  X18Plus = "X18+"
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


