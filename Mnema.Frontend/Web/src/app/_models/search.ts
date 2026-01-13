import {Provider} from "./page";

export type MetadataBag = { [key: string]: string[] };

export type SearchRequest = {
  provider: Provider;
  query: string;
  modifiers?: MetadataBag;
}

export type DownloadRequest = {
  provider: Provider;
  id: string;
  downloadUrl: string;
  baseDir: string;
  title: string;
  startImmediately: boolean;
  metadata: MetadataBag;
}

export type StopRequest = {
  provider: Provider;
  id: string;
  delete: boolean;
}
