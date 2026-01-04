import {Provider} from "./page";
import {MetadataBag} from "./search";

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
  metadata: MetadataBag;
}

export enum RefreshFrequency {
  Day = 2,
  Week,
  Month,
}
