import {Routes} from "@angular/router";
import {SubscriptionManagerComponent} from "../subscription-manager/subscription-manager.component";
import {NotificationsComponent} from "../notifications/notifications.component";

export const routes: Routes = [
  {
    path: 'subscriptions',
    component: SubscriptionManagerComponent
  },
  {
    path: 'notifications',
    component: NotificationsComponent
  }
]
