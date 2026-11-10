import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  EventEmitter,
  inject,
  signal
} from '@angular/core';
import {ModalService} from "../../../../_services/modal.service";
import {PageLoader} from "../../../../shared/_component/paginator/paginator.component";
import {finalize, map, of, switchMap} from "rxjs";
import {GenericFormModalComponent} from "../../../../generic-form/generic-form-modal/generic-form-modal.component";
import {DefaultModalOptions} from "../../../../_models/default-modal-options";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {DownloadClient, DownloadClientService, DownloadClientType} from "./download-client.service";
import {DownloadClientTypePipe} from "./_pipes/download-client-type.pipe";
import {NgbDropdownModule} from "@ng-bootstrap/ng-bootstrap";
import {ItemListComponent} from "@mnema/shared/_component/item-list/item-list.component";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";

@Component({
  selector: 'app-download-client-settings',
  imports: [
    DownloadClientTypePipe,
    TranslocoDirective,
    NgbDropdownModule,
    ItemListComponent,
  ],
  templateUrl: './download-client-settings.component.html',
  styleUrl: './download-client-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DownloadClientSettingsComponent {

  private readonly downloadClientService = inject(DownloadClientService);
  private readonly modalService = inject(ModalService);
  private readonly destroyRef = inject(DestroyRef);

  protected pageReloader = new EventEmitter<void>();
  protected freeTypes = signal<DownloadClientType[]>([]);

  pageLoader = computed<PageLoader<DownloadClient>>(() => {
    return (pageNumber: number, pageSize: number) => {
      return this.downloadClientService.getDownloadClients(pageNumber, pageSize);
    };
  });

  constructor() {
    this.refreshFreeTypes();
  }

  trackBy(_: number, client: DownloadClient) {
    return client.id;
  }

  edit(client: DownloadClient | null, type?: DownloadClientType) {
    if (client == null && type == null) return;

    const type$ = client == null ? of(type!) : of(client.type);

    type$.pipe(
      switchMap(type => this.downloadClientService.getForm(type).pipe(
        map(form => ({form, type}))
      )),
      switchMap(({form, type}) => {
        const [modal, component] = this.modalService.open(GenericFormModalComponent, DefaultModalOptions);

        component.double.set(true);
        component.formDefinition.set(form);
        component.initialValue.set(client ?? {type: type, metadata: {}});
        component.translationKey.set('settings.download-clients.edit');

        return this.modalService.onClose$<DownloadClient>(modal);
      }),
      switchMap(c => this.downloadClientService.updateDownloadClient(c)),
      finalize(() => {
        this.refreshFreeTypes();
        this.pageReloader.emit();
      }),
    ).subscribe()
  }

  releaseLock(client: DownloadClient) {
    this.modalService.confirm$({question: translate('settings.download-clients.release-lock.title'),
    }, true)
      .pipe(
        switchMap(() => this.downloadClientService.releaseLock(client.id)),
        finalize(() => this.pageReloader.emit()),
      ).subscribe();
  }

  remove(client: DownloadClient) {
    this.modalService.confirm$({question: translate('settings.download-clients.delete.title'),
    }, true)
      .pipe(
        switchMap(() => this.downloadClientService.deleteDownloadClient(client.id)),
        finalize(() => {
          this.refreshFreeTypes();
          this.pageReloader.emit();
        }),
      ).subscribe();
  }

  private refreshFreeTypes() {
    this.downloadClientService.getFreeTypes().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(types => this.freeTypes.set(types));
  }
}
