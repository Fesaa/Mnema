import {Routes} from "@angular/router";
import {SubscriptionManagerComponent} from "../subscription-manager/subscription-manager.component";
import {MonitoredSeriesManagerComponent} from "../monitored-series-manager/monitored-series-manager.component";
import {NotificationsComponent} from "../notifications/notifications.component";
import {ActiveDownloadsComponent} from "../dashboard/active-downloads/active-downloads.component";

export const routes: Routes = [
  {
    path: 'subscriptions',
    component: SubscriptionManagerComponent
  },
  {
    path: 'monitored-series',
    component: MonitoredSeriesManagerComponent
  },
  {
    path: 'notifications',
    component: NotificationsComponent
  },
  {
    path: 'active-downloads',
    component: ActiveDownloadsComponent
  }
]
