import {Routes} from "@angular/router";
import {SubscriptionManagerComponent} from "../subscription-manager/subscription-manager.component";
import {NotificationsComponent} from "../notifications/notifications.component";
import {ActiveDownloadsComponent} from "../dashboard/active-downloads/active-downloads.component";

export const routes: Routes = [
  {
    path: 'subscriptions',
    component: SubscriptionManagerComponent
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
