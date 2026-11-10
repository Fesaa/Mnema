import {ChangeDetectionStrategy, Component, computed, EventEmitter, inject} from '@angular/core';
import {AuthKey, AuthKeyService} from "@mnema/settings/settings/_components/auth-keys/auth-key.service";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {ModalService} from "@mnema/_services/modal.service";
import {PageLoader} from "@mnema/shared/_component/paginator/paginator.component";
import {finalize, switchMap} from "rxjs";
import {GenericFormModalComponent} from "@mnema/generic-form/generic-form-modal/generic-form-modal.component";
import {DefaultModalOptions} from "@mnema/_models/default-modal-options";
import {Clipboard} from "@angular/cdk/clipboard";
import {ToastService} from "@mnema/_services/toast.service";
import {ItemListComponent} from "@mnema/shared/_component/item-list/item-list.component";

@Component({
  selector: 'app-auth-keys',
  imports: [
    TranslocoDirective,
    ItemListComponent,
  ],
  templateUrl: './auth-keys.component.html',
  styleUrl: './auth-keys.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthKeysComponent {

  private readonly authKeyService = inject(AuthKeyService);
  private readonly modalService = inject(ModalService);
  private readonly clipboard = inject(Clipboard);
  private readonly toastService = inject(ToastService);

  protected pageReloader = new EventEmitter<void>();
  protected pageLoader = computed<PageLoader<AuthKey>>(() => {
    return (pageNumber: number, pageSize: number) => {
      return this.authKeyService.all(pageNumber, pageSize);
    }
  });

  protected trackBy(_: number, authKey: AuthKey) {
    return authKey.id;
  }

  edit(authKey: AuthKey | null) {
    this.authKeyService.form().pipe(
      switchMap(form => {
        const [modal, component] = this.modalService.open(GenericFormModalComponent, DefaultModalOptions);

        component.double.set(true);
        component.formDefinition.set(form);
        component.initialValue.set(authKey ?? {key: '', roles: [], id: '', name: ''});
        component.translationKey.set('settings.auth-keys.edit');

        return this.modalService.onClose$<AuthKey>(modal);
      }),
      switchMap(authKey => authKey.id ? this.authKeyService.update(authKey) : this.authKeyService.create(authKey)),
      finalize(() => this.pageReloader.emit()),
    ).subscribe();
  }

  remove(authKey: AuthKey) {
    this.modalService.confirm$(
      {question: translate('settings.auth-keys.confirm-delete')}, true)
      .pipe(
        switchMap(() => this.authKeyService.delete(authKey.id)),
        finalize(() => this.pageReloader.emit()),
      ).subscribe();
  }

  copy(authKey: AuthKey) {
    this.clipboard.copy(authKey.key);
    this.toastService.infoLoco('settings.auth-keys.copy', {}, {key: authKey.name})
  }
}
