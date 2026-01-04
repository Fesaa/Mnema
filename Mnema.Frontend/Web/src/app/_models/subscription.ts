import {Provider} from "./page";
import {MetadataBag} from "./search";

export type Subscription = {
  id: string;
  contentId: string;
  title: string;
  baseDir: string;
  provider: Provider;
  metadata: MetadataBag;
  status: SubscriptionStatus;
}

export enum SubscriptionStatus {
  Enabled = 0,
  Disabled = 1,
}
