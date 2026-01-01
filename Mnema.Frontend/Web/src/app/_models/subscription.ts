import {Provider} from "./page";
import {DownloadRequestMetadata} from "./search";

export type Subscription = {
  id: string;
  contentId: string;
  refreshFrequency: RefreshFrequency;
  title: string;
  baseDir: string;
  lastRun: Date;
  lastRunSuccess: boolean;
  nextRun: Date;
  provider: Provider;
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
