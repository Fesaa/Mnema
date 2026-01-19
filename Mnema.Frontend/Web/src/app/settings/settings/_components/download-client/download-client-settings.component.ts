import {ChangeDetectionStrategy, Component, computed, EventEmitter, inject} from '@angular/core';
import {ModalService} from "../../../../_services/modal.service";
import {PageLoader} from "../../../../shared/_component/paginator/paginator.component";
import {finalize, map, of, switchMap} from "rxjs";
import {GenericFormModalComponent} from "../../../../generic-form/generic-form-modal/generic-form-modal.component";
import {DefaultModalOptions} from "../../../../_models/default-modal-options";
import {ListSelectModalComponent} from "../../../../shared/_component/list-select-modal/list-select-modal.component";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {DownloadClient, DownloadClientService, DownloadClientType} from "./download-client.service";
import {DownloadClientTypePipe} from "./_pipes/download-client-type.pipe";
import {TableComponent} from "../../../../shared/_component/table/table.component";

@Component({
  selector: 'app-download-client-settings',
  imports: [
    TableComponent,
    DownloadClientTypePipe,
    TranslocoDirective
  ],
  templateUrl: './download-client-settings.component.html',
  styleUrl: './download-client-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DownloadClientSettingsComponent {

  private readonly downloadClientService = inject(DownloadClientService);
  private readonly modalService = inject(ModalService);

  protected pageReloader = new EventEmitter<void>();

  pageLoader = computed<PageLoader<DownloadClient>>(() => {
    return (pageNumber: number, pageSize: number) => {
      return this.downloadClientService.getDownloadClients(pageNumber, pageSize);
    };
  });

  trackBy(_: number, client: DownloadClient) {
    return client.id;
  }

  edit(client: DownloadClient | null) {
    const type$ = client == null
      ? this.promptForConnectionType() : of(client.type);

    type$.pipe(
      switchMap(type => this.downloadClientService.getForm(type).pipe(
        map(form => ({form, type}))
      )),
      switchMap(({form, type}) => {
        const [modal, component] = this.modalService.open(GenericFormModalComponent, DefaultModalOptions);

        component.double.set(true);
        component.formDefinition.set(form);
        component.initialValue.set(client ?? {type: type, metadata: {}});
        component.translationKey.set('settings.external-connections.edit');

        return this.modalService.onClose$<DownloadClient>(modal);
      }),
      switchMap(c => this.downloadClientService.updateDownloadClient(c)),
      finalize(() => this.pageReloader.emit()),
    ).subscribe()
  }

  private promptForConnectionType() {
    const typePipe = new DownloadClientTypePipe();

    return this.downloadClientService.getFreeTypes().pipe(
      map(types => types.map(type =>
        ({label: typePipe.transform(type), value: type}))),
      switchMap(types => {
        const [modal, component] = this.modalService.open(ListSelectModalComponent<DownloadClientType>, {
          size: "lg", centered: true,
        });
        component.title.set(translate('settings.external-connections.edit.select-type-title'));
        component.inputItems.set(types);

      return this.modalService.onClose$<DownloadClientType>(modal);
      })
    );
  }

  releaseLock(client: DownloadClient) {
    this.modalService.confirm$({question: translate('settings.external-connections.release-lock.title'),
    }, true)
      .pipe(
        switchMap(() => this.downloadClientService.releaseLock(client.id)),
        finalize(() => this.pageReloader.emit()),
      ).subscribe();
  }

  remove(client: DownloadClient) {
    this.modalService.confirm$({question: translate('settings.external-connections.delete.title'),
    }, true)
      .pipe(
        switchMap(() => this.downloadClientService.deleteDownloadClient(client.id)),
        finalize(() => this.pageReloader.emit()),
      ).subscribe();
  }

}
