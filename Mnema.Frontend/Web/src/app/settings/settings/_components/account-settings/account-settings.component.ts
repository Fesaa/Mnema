import {ChangeDetectionStrategy, ChangeDetectorRef, Component, computed, inject,} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators
} from "@angular/forms";
import {TranslocoDirective} from "@jsverse/transloco";
import {FormInputComponent} from "../../../../shared/form/form-input/form-input.component";
import {ToastService} from "../../../../_services/toast.service";
import {TitleCasePipe} from "@angular/common";
import {AccountService} from "../../../../_services/account.service";

@Component({
  selector: 'app-account-settings',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormInputComponent,
    TranslocoDirective,
    TitleCasePipe,
  ],
  templateUrl: './account-settings.component.html',
  styleUrl: './account-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AccountSettingsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly toastService = inject(ToastService);
  private readonly cdRef = inject(ChangeDetectorRef);
  private readonly accountService = inject(AccountService);

  private readonly user = computed(() => this.accountService.currentUser())

  accountForm: FormGroup = this.fb.group({
    email: this.fb.control(this.user()?.email, [Validators.required, Validators.email]),
    username: this.fb.control(this.user()?.name, [Validators.required])
  });

  passwordForm = this.fb.group({});

  constructor() {
    this.passwordForm.addControl('oldPassword', new FormControl('', [Validators.required, Validators.minLength(8)]));
    this.passwordForm.addControl('password', this.fb.control('', [Validators.required, Validators.minLength(8)]));
    this.passwordForm.addControl('confirmPassword', this.fb.control('', [Validators.required, this.sameAs(this.passwordForm, 'password')]));
  }

  getFormControl(form: FormGroup, path: string): FormControl | null {
    const ctrl = form.get(path);
    return ctrl instanceof FormControl ? ctrl : null;
  }

  saveAccount() {
    if (this.accountForm.invalid) {
      this.toastService.errorLoco('account.errors.invalid');
      return;
    }

    if (!this.accountForm.dirty) {
      this.toastService.warningLoco('shared.toasts.no-changes');
      return;
    }

    this.accountService.updateMe(this.accountForm.getRawValue()).subscribe({
      next: () => {
        this.toastService.successLoco('account.success.update');
      },
      error: err => {
        console.error(err);
        this.toastService.errorLoco('account.errors.update', {}, {error: err.error.error});
      }
    });
  }

  savePassword() {
    if (this.passwordForm.invalid) {
      this.toastService.errorLoco('account.errors.passwordInvalid');
      return;
    }

    const dto: any = this.passwordForm.getRawValue();
    this.accountService.updatePassword({oldPassword: dto.oldPassword, newPassword: dto.password}).subscribe({
      next: () => {
        this.toastService.successLoco('account.success.password');
      },
      error: err => {
        console.error(err);
        this.toastService.errorLoco('account.errors.password', {}, {error: err.error.error});
      }
    })

    this.passwordForm.reset();
  }

  private sameAs(form: FormGroup, name: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const other = form.get(name);
      if (!other) return null;

      const same = control.value === other.value;
      if (same) return null;

      return {'sameAs': {'other': name, 'otherValue': other.value}}
    }
  }
}
