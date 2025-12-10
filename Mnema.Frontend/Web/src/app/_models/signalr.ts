import {ContentState, SpeedType} from "./stats";
import {Provider} from "./page";

export type ContentSizeUpdate = {
  contentId: string;
  size: string;
};

export type ContentProgressUpdate = {
  contentId: string;
  progress: number;
  estimated?: number;
  speed_type: SpeedType;
  speed: number;
};

export type AddContent = {
  provider: Provider;
  contentId: string;
  contentState: ContentState;
  name: string;
  ref_url: string;
  size: string;
  downloading: boolean;
  progress: number;
  estimated?: number;
  speed_type: SpeedType;
  speed: number;
  download_dir: string;
};

export type DeleteContent = {
  contentId: string;
};

export type ContentStateUpdate = {
  contentId: string;
  contentState: ContentState;
};
