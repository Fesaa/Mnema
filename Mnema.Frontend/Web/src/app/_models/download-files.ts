import {ExternalDownloadFile} from "./external-download-file";

export type DownloadFiles = {
  files: ExternalDownloadFile[];
  monitoredChapters: MonitoredChapterOption[] | null;
}

export type MonitoredChapterOption = {
  id: string;
  volume: string;
  chapter: string;
  title: string;
  status: number;
}
