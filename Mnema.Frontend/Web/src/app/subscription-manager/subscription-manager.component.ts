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
  private readonly subscriptionService = inject(SubscriptionService);
  private readonly toastService = inject(ToastService);
  private readonly pageService = inject(PageService);
  private readonly providerNamePipe = inject(ProviderNamePipe);
  private readonly utilityService = inject(UtilityService);

  metadata = signal<Map<Provider, DownloadMetadata>>(new Map());
  allowedProviders = signal<Provider[]>([]);
  subscriptions = signal<Subscription[]>([]);
  hasRanAll = signal(false);
  filterText = signal('');

  filteredSubscriptions = computed(() => {
    const filter = this.filterText();
    const subs = this.subscriptions();

    if (!filter) return subs;

    const normalizedFilter = this.utilityService.normalize(filter);
    return subs.filter(s => this.utilityService.normalize(s.title).includes(normalizedFilter));
  })

  constructor() {
    effect(() => {
      const providers = this.allowedProviders();
      for (const provider of providers) {
        this.pageService.metadata(provider).subscribe({
          next: metadata => {
            this.metadata.update(m => {
              m.set(provider, metadata);
              return m;
            })
          },
          error: error => {
            this.toastService.errorLoco("page.toasts.metadata-failed",
              {provider: this.providerNamePipe.transform(provider)}, {msg: error.error.message});
          }
        })
      }
    });
  }

  ngOnInit(): void {
    this.navService.setNavVisibility(true);

    forkJoin([
      this.subscriptionService.all(),
      this.subscriptionService.providers(),
    ]).subscribe(([s, providers]) => {
      this.subscriptions.set(s ?? [])
      this.allowedProviders.set(providers ?? [])
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
    if (sub.ID == 0) {
      return
    }

    this.subscriptionService.runOnce(sub.ID).subscribe({
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


    this.subscriptionService.delete(sub.ID).subscribe({
      next: () => {
        this.subscriptions.update(subs => subs.filter(s => s.ID !== sub.ID));
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
    return `${sub.ID}`
  }

  edit(sub: Subscription) {
    const [modal, component] = this.modalService.open(EditSubscriptionModalComponent, DefaultModalOptions);
    component.subscription.set(sub);
    component.providers.set(this.allowedProviders());
    component.metadata.set(this.metadata().get(sub.provider) ?? {definitions: []});

    modal.result.then(() => this.subscriptionService.all().subscribe(subs => {
      this.subscriptions.set(subs);
    }));
  }
}
