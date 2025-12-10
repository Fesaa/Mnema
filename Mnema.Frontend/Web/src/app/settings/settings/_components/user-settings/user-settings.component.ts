import {Component, inject, OnInit, signal} from '@angular/core';
import {AccountService} from "../../../../_services/account.service";
import {User, UserDto} from "../../../../_models/user";
import {Clipboard} from "@angular/cdk/clipboard";
import {FormsModule} from "@angular/forms";
import {ToastService} from '../../../../_services/toast.service';
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {TableComponent} from "../../../../shared/_component/table/table.component";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {ModalService} from "../../../../_services/modal.service";
import {EditUserModalComponent} from "./_components/edit-user-modal/edit-user-modal.component";
import {DefaultModalOptions} from "../../../../_models/default-modal-options";
import {PageService} from "../../../../_services/page.service";

@Component({
  selector: 'app-user-settings',
  imports: [
    FormsModule,
    TranslocoDirective,
    TableComponent,
    NgbTooltip
  ],
  templateUrl: './user-settings.component.html',
  styleUrl: './user-settings.component.scss'
})
export class UserSettingsComponent implements OnInit {

  private readonly modalService = inject(ModalService);
  private readonly accountService = inject(AccountService);
  private readonly pageService = inject(PageService);
  private readonly toastService = inject(ToastService);
  private readonly clipBoard = inject(Clipboard);

  users = signal<UserDto[]>([]);
  pages = this.pageService.pages;
  authUser = this.accountService.currentUser();

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers() {
    this.accountService.all().subscribe({
      next: users => {
        this.users.set(users);
      },
      error: err => {
        this.toastService.genericError(err.error.message);
      }
    });
  }

  copyApiKey() {
    this.clipBoard.copy(this.authUser!.apiKey)
  }

  async resetApiKey() {
    if (!await this.modalService.confirm({
      question: translate("settings.users.confirm-reset-api-key")
    })) {
      return;
    }

    this.accountService.refreshApiKey().subscribe({
      next: res => {
        this.clipBoard.copy(res.ApiKey)
        this.toastService.successLoco("settings.users.toasts.reset-api-key.success");
      },
      error: err => {
        this.toastService.errorLoco("settings.users.toasts.reset-api-key.error", {}, {msg: err.error.message});
      }
    })
  }

  async resetPassword(user: UserDto) {
    if (!await this.modalService.confirm({
      question: translate("settings.users.confirm-reset-password", {name: user.name})
    })) {
      return;
    }

    this.accountService.generateReset(user.id).subscribe({
      next: reset => {
        this.clipBoard.copy(`/login/reset?key=${reset.Key}`)
        this.toastService.successLoco("settings.users.toasts.reset-password.success");
      },
      error: err => {
        this.toastService.errorLoco("settings.users.toasts.reset-password.error", {}, {msg: err.error.message});
      }
    })
  }

  async deleteUser(user: UserDto) {
    if (!await this.modalService.confirm({
      question: translate("settings.users.confirm-delete", {name: user.name})
    })) {
      return;
    }

    this.accountService.delete(user.id).subscribe({
      next: _ => {
        this.users.update(users => users.filter(dto => dto.id !== user.id));
        this.toastService.successLoco("settings.users.toasts.delete.success", {name: user.name});
      },
      error: err => {
        this.toastService.errorLoco("settings.users.toasts.delete.error",
          {name: user.name}, {msg: err.error.message});
      }
    })
  }

  emptyUserPresent() {
    return this.users().find(user => user.id === 0) !== undefined;
  }

  trackBy(idx: number, user: UserDto) {
    return `${user.id}`
  }

  edit(user: UserDto | null) {
    const [modal, component] = this.modalService.open(EditUserModalComponent, DefaultModalOptions);
    component.user.set(user ?? {
      id: -1,
      name: '',
      email: '',
      canDelete: false,
      roles: [],
      pages: [],
    });
    component.pages.set(this.pages());

    modal.result.then(() => this.loadUsers());
  }
}
