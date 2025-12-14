import {Provider} from "./page";

export type InfoStat = {
  provider: Provider;
  id: string;
  contentState: ContentState;
  name: string;
  refUrl: string;
  size: string;
  downloading: boolean;
  progress: number;
  estimated?: number;
  speed_type: SpeedType;
  speed: number;
  downloadDir: string;
}

export enum ContentState {
  Queued = 0,
  Loading = 1,
  Waiting = 2,
  Ready = 3,
  Downloading = 4,
  Cleanup = 5,
}

export enum SpeedType {
  BYTES,
  VOLUMES,
  IMAGES,
}

export type StatsResponse = {
  running: InfoStat[];
}
