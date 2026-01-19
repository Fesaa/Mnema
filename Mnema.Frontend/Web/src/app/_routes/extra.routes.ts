import {Routes} from "@angular/router";
import {SubscriptionManagerComponent} from "../subscription-manager/subscription-manager.component";
import {MonitoredSeriesManagerComponent} from "../monitored-series-manager/monitored-series-manager.component";
import {NotificationsComponent} from "../notifications/notifications.component";
import {ActiveDownloadsComponent} from "../dashboard/active-downloads/active-downloads.component";
import {SeriesSearchComponent} from "../monitored-series-manager/_components/series-search/series-search.component";

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
    path: 'series-search',
    component: SeriesSearchComponent,
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
