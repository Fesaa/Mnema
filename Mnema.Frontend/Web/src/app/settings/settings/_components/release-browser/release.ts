export interface ContentReleaseDto {
  /** The id in the database */
  id: string;

  /** The id that uniquely defines this content release */
  releaseId: string;

  /** The id that uniquely defines the content this release is part of */
  contentId?: string | null;

  /** Name of the release (e.g., chapter name) */
  releaseName: string;

  /** Name of the content (e.g., series name) */
  contentName: string;

  /** Time this release was published */
  releaseDate: string; // ISO 8601 string

  createdUtc: string;
  lastModifiedUtc: string;
}
