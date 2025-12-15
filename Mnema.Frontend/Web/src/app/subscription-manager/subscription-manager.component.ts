import {Component, computed, effect, inject, OnInit, signal} from '@angular/core';
import {NavService} from "../_services/nav.service";
import {SubscriptionService} from '../_services/subscription.service';
import {RefreshFrequency, Subscription} from "../_models/subscription";
import {DownloadMetadata, Provider} from "../_models/page";
import {dropAnimation} from "../_animations/drop-animation";
import {SubscriptionExternalUrlPipe} from "../_pipes/subscription-external-url.pipe";
import {DatePipe} from "@angular/common";
import {RefreshFrequencyPipe} from "../_pipes/refresh-frequency.pipe";
import {ToastService} from "../_services/toast.service";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {UtcToLocalTimePipe} from "../_pipes/utc-to-local.pipe";
import {TableComponent} from "../shared/_component/table/table.component";
import {BadgeComponent} from "../shared/_component/badge/badge.component";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {ModalService} from "../_services/modal.service";
import {forkJoin} from "rxjs";
import {EditSubscriptionModalComponent} from "./_components/edit-subscription-modal/edit-subscription-modal.component";
import {DefaultModalOptions} from "../_models/default-modal-options";
import {PageService} from "../_services/page.service";
import {ProviderNamePipe} from "../_pipes/provider-name.pipe";
import {UtilityService} from "../_services/utility.service";

@Component({
  selector: 'app-subscription-manager',
  imports: [
    SubscriptionExternalUrlPipe,
    DatePipe,
    RefreshFrequencyPipe,
    TranslocoDirective,
    UtcToLocalTimePipe,
    TableComponent,
    BadgeComponent,
    NgbTooltip,
  ],
  templateUrl: './subscription-manager.component.html',
  styleUrl: './subscription-manager.component.scss',
  animations: [dropAnimation]
})
export class SubscriptionManagerComponent implements OnInit {

  private readonly modalService = inject(ModalService);
  private readonly navService = inject(NavService);
  protected readonly subscriptionService = inject(SubscriptionService);
  private readonly toastService = inject(ToastService);
  private readonly pageService = inject(PageService);
  private readonly providerNamePipe = inject(ProviderNamePipe);
  private readonly utilityService = inject(UtilityService);

  metadata = signal<Map<Provider, DownloadMetadata>>(new Map());
  allowedProviders = signal<Provider[]>([]);
  hasRanAll = signal(false);
  filterText = signal('');

  constructor() {
    effect(() => {
      const providers = this.allowedProviders();
      for (const provider of providers) {
        this.pageService.metadata(provider).subscribe(metadata => {
          this.metadata.update(m => {
            m.set(provider, metadata);
            return m;
          });
        });
      }
    });
  }

  pageLoader(pn: number, ps: number) {
    return this.subscriptionService.all(pn, ps);
  }

  ngOnInit(): void {
    this.navService.setNavVisibility(true);

    this.subscriptionService.providers().subscribe(providers => {
      this.allowedProviders.set(providers ?? []);
    });
  }

  updateFilter(event: Event) {
    const target = event.target as HTMLInputElement;
    this.filterText.set(target.value);
  }

  runAll() {
    if (this.hasRanAll()) return;

    this.hasRanAll.set(true);
    this.subscriptionService.runAll().subscribe({
      next: (result) => {
        this.toastService.successLoco("subscriptions.actions.run-all-success")
      },
      error: (error) => {
        console.error(error);
        this.toastService.genericError(error);
      }
    })
  }

  runOnce(sub: Subscription) {
    if (sub.id == 0) {
      return
    }

    this.subscriptionService.runOnce(sub.id).subscribe({
      next: () => {
        this.toastService.successLoco("subscriptions.toasts.run-once.success", {}, {name: sub.title});
      },
      error: (err) => {
        this.toastService.errorLoco("subscriptions.toasts.run-once.error", {name: sub.title}, {msg: err.error.message});
      }
    })
  }

  async delete(sub: Subscription) {
    if (!await this.modalService.confirm({
      question: translate("subscriptions.confirm-delete", {title: sub.title})
    })) {
      return;
    }


    this.subscriptionService.delete(sub.id).subscribe({
      next: () => {
        this.toastService.successLoco("subscriptions.toasts.delete.success", {name: sub.title});
      },
      error: err => {
        this.toastService.errorLoco("subscriptions.toasts.delete.error", {name: sub.title}, {msg: err.error.message});
      }
    })
  }

  getSeverity(sub: Subscription): "primary" | "secondary" | "error" | "warning" {
    switch (sub.refreshFrequency) {
      case RefreshFrequency.Day:
        return "primary"
      case RefreshFrequency.Week:
        return "warning"
      case RefreshFrequency.Month:
        return "error"
    }
  }

  trackBy(idx: number, sub: Subscription) {
    return `${sub.id}`
  }

  edit(sub: Subscription) {
    const [modal, component] = this.modalService.open(EditSubscriptionModalComponent, DefaultModalOptions);
    component.subscription.set(sub);
    component.providers.set(this.allowedProviders());
    component.metadata.set(this.metadata().get(sub.provider) ?? {definitions: []});
  }
}
