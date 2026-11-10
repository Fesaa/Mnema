import {ChangeDetectionStrategy, Component, computed, EventEmitter, inject} from '@angular/core';
import {ModalService} from "../../../../_services/modal.service";
import {Connection, ConnectionEvent, ConnectionService, ConnectionType, ConnectionTypes} from "./connection.service";
import {PageLoader} from "../../../../shared/_component/paginator/paginator.component";
import {ConnectionTypePipe} from "./_pipes/connection-type.pipe";
import {ConnectionEventPipe} from "./_pipes/connection-event.pipe";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {GenericFormModalComponent} from "../../../../generic-form/generic-form-modal/generic-form-modal.component";
import {DefaultModalOptions} from "../../../../_models/default-modal-options";
import {filter, finalize, map, of, switchMap} from "rxjs";
import {NotificationService} from "@mnema/_services/notification.service";
import {NgbDropdownModule, NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {ItemListComponent} from "@mnema/shared/_component/item-list/item-list.component";

@Component({
  selector: 'app-external-connection-settings',
  imports: [
    ConnectionTypePipe,
    ConnectionEventPipe,
    TranslocoDirective,
    NgbDropdownModule,
    NgbTooltip,
    ItemListComponent,
  ],
  templateUrl: './connection-settings.component.html',
  styleUrl: './connection-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConnectionSettingsComponent {

  private readonly connectionService = inject(ConnectionService);
  private readonly notificationService = inject(NotificationService);
  private readonly modalService = inject(ModalService);
  private readonly connectionEventPipe = new ConnectionEventPipe();

  protected readonly ConnectionTypes = ConnectionTypes;
  protected pageReloader = new EventEmitter<void>();

  pageLoader = computed<PageLoader<Connection>>(() => {
    return (pageNumber: number, pageSize: number) => {
      return this.connectionService.getConnections(pageNumber, pageSize);
    };
  });

  trackBy(_: number, connection: Connection) {
    return connection.id;
  }

  protected visibleEvents(connection: Connection): ConnectionEvent[] {
    const count = connection.followedEvents.length > 3 ? 2 : connection.followedEvents.length;
    return connection.followedEvents.slice(0, count);
  }

  protected extraEvents(connection: Connection): ConnectionEvent[] {
    return connection.followedEvents.slice(this.visibleEvents(connection).length);
  }

  protected extraEventsLabel(connection: Connection): string {
    return this.extraEvents(connection)
      .map(event => this.connectionEventPipe.transform(event))
      .join(', ');
  }

  edit(connection: Connection | null, type?: ConnectionType) {
    if (connection == null && type == null) return;

    const type$ = connection == null ? of(type!) : of(connection.type);

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
      switchMap(c => this.connectionService.updateConnection(c).pipe(
        filter(() => c.type === ConnectionType.Native),
        switchMap(() => this.notificationService.enabled()),
      )),
      finalize(() => this.pageReloader.emit()),
    ).subscribe()
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
