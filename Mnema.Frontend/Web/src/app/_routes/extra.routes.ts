import {Routes} from "@angular/router";
import {SubscriptionManagerComponent} from "../subscription-manager/subscription-manager.component";
import {NotificationsComponent} from "../notifications/notifications.component";
import {ActiveDownloadsComponent} from "../dashboard/active-downloads/active-downloads.component";
import {
  MonitoredSeriesManagerComponent
} from "@mnema/features/monitored-series/manager/monitored-series-manager.component";
import {SeriesSearchComponent} from "@mnema/features/monitored-series/series-search/series-search.component";
import {MonitoredSeriesComponent} from "@mnema/features/monitored-series/monitored-series/monitored-series.component";
import {
  monitoredSeriesResolver
} from "@mnema/features/monitored-series/monitored-series/monitored-series.resolver";

export const routes: Routes = [
  {
    path: 'subscriptions',
    component: SubscriptionManagerComponent
  },
  {
    path: 'monitored-series',
    component: MonitoredSeriesManagerComponent,
  },
  {
    path: 'monitored-series-detail/:id',
    component: MonitoredSeriesComponent,
    resolve: {
      series: monitoredSeriesResolver
    }
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
