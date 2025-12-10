import {ChangeDetectionStrategy, Component, computed, effect, inject, model, OnInit, signal} from '@angular/core';
import {AllRoles, Role, UserDto} from "../../../../../../_models/user";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {AccountService} from "../../../../../../_services/account.service";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {SettingsItemComponent} from "../../../../../../shared/form/settings-item/settings-item.component";
import {
  Form,
  FormArray,
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators
} from "@angular/forms";
import {TypeaheadComponent, TypeaheadSettings} from "../../../../../../type-ahead/typeahead.component";
import {of} from "rxjs";
import {DefaultValuePipe} from "../../../../../../_pipes/default-value.pipe";
import {ToastService} from "../../../../../../_services/toast.service";
import {RolePipe} from "../../../../../../_pipes/role.pipe";
import {Page} from "../../../../../../_models/page";

@Component({
  selector: 'app-edit-user-modal',
  imports: [
    TranslocoDirective,
    SettingsItemComponent,
    ReactiveFormsModule,
    TypeaheadComponent,
    DefaultValuePipe,
    RolePipe
  ],
  templateUrl: './edit-user-modal.component.html',
  styleUrl: './edit-user-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditUserModalComponent implements OnInit {

  private readonly toastService = inject(ToastService);
  private readonly userService = inject(AccountService);
  private readonly modal = inject(NgbActiveModal);
  private readonly rolePipe = inject(RolePipe);
  private readonly fb = inject(NonNullableFormBuilder);

  user = model.required<UserDto>();
  pages = model.required<Page[]>();

  userName = computed(() => {
    const user = this.user();
    if (user.name) return user.name;

    return translate('edit-user-modal.someone');
  });


  userForm!: FormGroup<{
    id: FormControl<number>,
    name: FormControl<string>,
    email: FormControl<string>,
    roles: FormArray<FormControl<Role>>
    pages: FormArray<FormGroup<{
      id: FormControl<number>;
      checked: FormControl<boolean>;
    }>>
  }>;

  get pagesFormArray() {
    return this.userForm.get('pages') as FormArray<FormGroup<{
      id: FormControl<number>;
      checked: FormControl<boolean>;
    }>>;
  }

  getPageName(id: number) {
    return this.pages().find(p => p.id === id)?.title;
  }

  ngOnInit() {
    const user = this.user();

    this.userForm = this.fb.group({
      id: this.fb.control(user.id),
      name: this.fb.control(user.name, [Validators.required]),
      email: this.fb.control(user.email),
      roles: this.fb.array(user.roles.map(r => this.fb.control(r))),
      pages: this.fb.array(this.pages().map(p => this.fb.group({
        id: this.fb.control(p.id),
        checked: this.fb.control(user.pages.length === 0 || user.pages.includes(p.id))
      }))),
    });
  }

  rolesTypeaheadSettings(): TypeaheadSettings<Role> {
    const settings = new TypeaheadSettings<Role>();
    settings.multiple = true;
    settings.minCharacters = 0;
    settings.id = 'role-typeahead';

    settings.fetchFn = (f) =>
      of(AllRoles.filter(p => this.rolePipe.transform(p).includes(f)));
    settings.savedData = this.user().roles;


    return settings;
  }

  updatePerms(perms: Role[] | Role) {
    this.userForm.get('roles')!.setValue(perms as Role[])
  }

  close() {
    this.modal.close();
  }

  packData() {
    const data = this.userForm.value as UserDto;

    data.id = data.id === -1 ? 0 : data.id;
    data.pages = (data.pages as unknown as {id: number, checked: boolean}[])
      .filter(x => x.checked)
      .map(x => x.id);
    return data;
  }

  save() {
    const user = this.packData();

    this.userService.updateOrCreate(user).subscribe({
      next: () => this.toastService.infoLoco("settings.users.toasts.updated.success", {name: this.userName()}),
      error: (err) => this.toastService.errorLoco("settings.users.toasts.update.error",
        {name: this.userName()}, {msg: err.error.message})
    }).add(() => this.close());
  }

}
