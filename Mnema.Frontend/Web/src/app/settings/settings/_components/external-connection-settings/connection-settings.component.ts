import {ChangeDetectionStrategy, Component, computed, EventEmitter, inject} from '@angular/core';
import {ModalService} from "../../../../_services/modal.service";
import {
  Connection,
  ConnectionService,
  ConnectionType,
  ConnectionTypes
} from "./connection.service";
import {TableComponent} from "../../../../shared/_component/table/table.component";
import {PageLoader} from "../../../../shared/_component/paginator/paginator.component";
import {ConnectionTypePipe} from "./_pipes/connection-type.pipe";
import {ConnectionEventPipe} from "./_pipes/connection-event.pipe";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {GenericFormModalComponent} from "../../../../generic-form/generic-form-modal/generic-form-modal.component";
import {DefaultModalOptions} from "../../../../_models/default-modal-options";
import {finalize, map, of, switchMap, take, takeUntil, tap} from "rxjs";
import {ListSelectModalComponent} from "../../../../shared/_component/list-select-modal/list-select-modal.component";

@Component({
  selector: 'app-external-connection-settings',
  imports: [
    TableComponent,
    ConnectionTypePipe,
    ConnectionEventPipe,
    TranslocoDirective
  ],
  templateUrl: './connection-settings.component.html',
  styleUrl: './connection-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConnectionSettingsComponent {

  private readonly connectionService = inject(ConnectionService);
  private readonly modalService = inject(ModalService);

  protected pageReloader = new EventEmitter<void>();

  pageLoader = computed<PageLoader<Connection>>(() => {
    return (pageNumber: number, pageSize: number) => {
      return this.connectionService.getConnections(pageNumber, pageSize);
    };
  });

  trackBy(_: number, connection: Connection) {
    return connection.id;
  }

  edit(connection: Connection | null) {
    const type$ = connection == null
      ? this.promptForConnectionType() : of(connection.type);

    type$.pipe(
    switchMap(type => this.connectionService.getConnectionForm(type).pipe(
      map(form => ({form, type}))
    )),
    switchMap(({form, type}) => {
        const [modal, component] = this.modalService.open(GenericFormModalComponent, DefaultModalOptions);

        component.double.set(true);
        component.formDefinition.set(form);
        component.initialValue.set(connection ?? {type: type, metadata: {}});
        component.translationKey.set('settings.connections.edit');

        return this.modalService.onClose$<Connection>(modal);
      }),
      switchMap(c => this.connectionService.updateConnection(c)),
      finalize(() => this.pageReloader.emit()),
    ).subscribe()
  }

  private promptForConnectionType() {
    const typePipe = new ConnectionTypePipe();

    const [modal, component] = this.modalService.open(ListSelectModalComponent<ConnectionType>, {
      size: "lg", centered: true,
    });
    component.title.set(translate('settings.connections.edit.select-type-title'));
    component.inputItems.set(ConnectionTypes.map(type => ({
      label: typePipe.transform(type),
      value: type
    })));

    return this.modalService.onClose$<ConnectionType>(modal);
  }

  remove(connection: Connection) {
    this.modalService.confirm$({question: translate('settings.connections.delete.title'),
    }, true)
      .pipe(
        switchMap(() => this.connectionService.deleteConnection(connection.id)),
        finalize(() => this.pageReloader.emit()),
    ).subscribe();

  }

}
