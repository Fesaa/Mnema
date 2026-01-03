import {ChangeDetectionStrategy, Component, computed, EventEmitter, inject} from '@angular/core';
import {ModalService} from "../../../../_services/modal.service";
import {
  ExternalConnection,
  ExternalConnectionService,
  ExternalConnectionType,
  ExternalConnectionTypes
} from "./external-connection.service";
import {TableComponent} from "../../../../shared/_component/table/table.component";
import {PageLoader} from "../../../../shared/_component/paginator/paginator.component";
import {ExternalConnectionTypePipe} from "./_pipes/external-connection-type.pipe";
import {ExternalConnectionEventPipe} from "./_pipes/external-connection-event.pipe";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {GenericFormModalComponent} from "../../../../generic-form/generic-form-modal/generic-form-modal.component";
import {DefaultModalOptions} from "../../../../_models/default-modal-options";
import {finalize, map, of, switchMap, take, takeUntil, tap} from "rxjs";
import {ListSelectModalComponent} from "../../../../shared/_component/list-select-modal/list-select-modal.component";

@Component({
  selector: 'app-external-connection-settings',
  imports: [
    TableComponent,
    ExternalConnectionTypePipe,
    ExternalConnectionEventPipe,
    TranslocoDirective
  ],
  templateUrl: './external-connection-settings.component.html',
  styleUrl: './external-connection-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExternalConnectionSettingsComponent {

  private readonly externalConnectionService = inject(ExternalConnectionService);
  private readonly modalService = inject(ModalService);

  protected pageReloader = new EventEmitter<void>();

  pageLoader = computed<PageLoader<ExternalConnection>>(() => {
    return (pageNumber: number, pageSize: number) => {
      return this.externalConnectionService.getExternalConnections(pageNumber, pageSize);
    };
  });

  trackBy(_: number, connection: ExternalConnection) {
    return connection.id;
  }

  edit(connection: ExternalConnection | null) {
    const type$ = connection == null
      ? this.promptForConnectionType() : of(connection.type);

    type$.pipe(
    switchMap(type => this.externalConnectionService.getConnectionForm(type).pipe(
      map(form => ({form, type}))
    )),
    switchMap(({form, type}) => {
        const [modal, component] = this.modalService.open(GenericFormModalComponent, DefaultModalOptions);

        component.double.set(true);
        component.formDefinition.set(form);
        component.initialValue.set(connection ?? {type: type, metadata: {}});
        component.translationKey.set('settings.external-connections.edit');

        return this.modalService.onClose$<ExternalConnection>(modal);
      }),
      switchMap(c => this.externalConnectionService.updateExternalConnection(c)),
      finalize(() => this.pageReloader.emit()),
    ).subscribe()
  }

  private promptForConnectionType() {
    const typePipe = new ExternalConnectionTypePipe();

    const [modal, component] = this.modalService.open(ListSelectModalComponent<ExternalConnectionType>, {
      size: "lg", centered: true,
    });
    component.title.set(translate('settings.external-connections.edit.select-type-title'));
    component.inputItems.set(ExternalConnectionTypes.map(type => ({
      label: typePipe.transform(type),
      value: type
    })));

    return this.modalService.onClose$<ExternalConnectionType>(modal);
  }

  remove(connection: ExternalConnection) {
    this.modalService.confirm$({question: translate('settings.external-connections.delete.title'),
    }, true)
      .pipe(
        switchMap(() => this.externalConnectionService.deleteExternalConnection(connection.id)),
        finalize(() => this.pageReloader.emit()),
    ).subscribe();

  }

}
