export type ExternalDownloadFile = {
  path: string;
  fileName: string;
  fileSize: number;
  selected: boolean;
  volumeMarker: string | null;
  chapterMarker: string | null;
  monitoredChapterId: string | null;
}
