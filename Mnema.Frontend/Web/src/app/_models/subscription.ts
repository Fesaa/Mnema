import {Provider} from "./page";
import {DownloadRequestMetadata} from "./search";

export type Subscription = {
  id: number;
  provider: Provider;
  contentId: string;
  refreshFrequency: RefreshFrequency;
  title: string;
  description?: string;
  baseDir: string;
  lastDownloadDir: string;
  lastCheck: Date;
  lastCheckSuccess: boolean;
  nextExecution: Date;
  metadata: DownloadRequestMetadata;
}

export enum RefreshFrequency {
  Day = 2,
  Week,
  Month,
}

export const RefreshFrequencies = [
  {label: "Day", value: RefreshFrequency.Day},
  {label: "Week", value: RefreshFrequency.Week},
  {label: "Month", value: RefreshFrequency.Month},
];
