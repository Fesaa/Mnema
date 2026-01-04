import {Component, computed, effect, EventEmitter, inject, OnInit, signal} from '@angular/core';
import {NavService} from "../_services/nav.service";
import {SubscriptionService} from '../_services/subscription.service';
import {Subscription} from "../_models/subscription";
import {Provider} from "../_models/page";
import {dropAnimation} from "../_animations/drop-animation";
import {SubscriptionExternalUrlPipe} from "../_pipes/subscription-external-url.pipe";
import {DatePipe} from "@angular/common";
import {ToastService} from "../_services/toast.service";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {UtcToLocalTimePipe} from "../_pipes/utc-to-local.pipe";
import {TableComponent} from "../shared/_component/table/table.component";
import {BadgeComponent} from "../shared/_component/badge/badge.component";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {ModalService} from "../_services/modal.service";
import {catchError, debounceTime, distinctUntilChanged, forkJoin, map, of, switchMap, tap} from "rxjs";
import {EditSubscriptionModalComponent} from "./_components/edit-subscription-modal/edit-subscription-modal.component";
import {DefaultModalOptions} from "../_models/default-modal-options";
import {PageService} from "../_services/page.service";
import {FormControl, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {takeUntilDestroyed, toSignal} from "@angular/core/rxjs-interop";
import {FormControlDefinition} from "../generic-form/form";
import {ProviderNamePipe} from "../_pipes/provider-name.pipe";

@Component({
  selector: 'app-subscription-manager',
  imports: [
    SubscriptionExternalUrlPipe,
    TranslocoDirective,
    TableComponent,
    NgbTooltip,
    ReactiveFormsModule,
    ProviderNamePipe,
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

  metadata = signal<Map<Provider, FormControlDefinition[]>>(new Map());
  allowedProviders = signal<Provider[]>([]);
  hasAny = signal(false);

  pageLoader = computed(() => {
    const filter = this.filter();

    return (pn: number, ps: number) => {
      return this.subscriptionService.all(filter.filterText ?? '', pn, ps);
    }
  });

  filterForm = new FormGroup({
    filterText: new FormControl(''),
  });
  filter = toSignal(this.filterForm.valueChanges.pipe(
    debounceTime(400),
    takeUntilDestroyed(),
    distinctUntilChanged(),
  ), { initialValue: { filterText: '' } });

  pageReloader = new EventEmitter<void>();

  ngOnInit(): void {
    this.navService.setNavVisibility(true);

    this.subscriptionService.providers()
      .pipe(
        tap(providers => this.allowedProviders.set(providers ?? [])),
        switchMap(providers => {
          const loaders$ = providers.map(
            p => this.pageService.metadata(p).pipe(
              map(m => [p, m] as [Provider, FormControlDefinition[]]),
              catchError(err => of([p, []] as [Provider, FormControlDefinition[]]))
            ));

          return forkJoin(loaders$);
        }),
        tap(metadata => this.metadata.set(new Map(metadata)))
      ).subscribe();
  }

  runOnce(sub: Subscription) {
    if (sub.id === '') {
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


    this.subscriptionService.delete(sub.id).pipe(
      tap(() => {
        this.toastService.successLoco("subscriptions.toasts.delete.success", {name: sub.title});
        this.pageReloader.emit();
      })
    ).subscribe();
  }

  trackBy(idx: number, sub: Subscription) {
    return sub.id
  }

  edit(sub: Subscription) {
    const [modal, component] = this.modalService.open(EditSubscriptionModalComponent, DefaultModalOptions);
    component.subscription.set(sub);
    component.providers.set(this.allowedProviders());
    component.metadata.set(this.metadata().get(sub.provider) ?? []);

    this.modalService.onClose$(modal, false).pipe(
      tap(() => this.pageReloader.emit())
    ).subscribe();
  }
}
