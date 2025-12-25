import {Provider} from "./page";

export type SearchRequest = {
  provider: Provider;
  query: string;
  modifiers?: { [key: string]: string[] };
}

export type DownloadRequest = {
  provider: Provider;
  id: string;
  dir: string;
  title: string;
  downloadMetadata: DownloadRequestMetadata;
}

export type DownloadRequestMetadata = {
  startImmediately: boolean;
  extra: { [key: string]: string[] };
}

export type StopRequest = {
  provider: Provider;
  id: string;
  delete: boolean;
}
