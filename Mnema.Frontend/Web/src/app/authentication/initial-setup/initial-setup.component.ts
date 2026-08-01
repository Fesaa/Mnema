import {ChangeDetectionStrategy, Component, signal} from '@angular/core';
import {translate, TranslocoDirective} from "@jsverse/transloco";

@Component({
  selector: 'app-initial-setup',
  imports: [
    TranslocoDirective
  ],
  templateUrl: './initial-setup.component.html',
  styleUrl: './initial-setup.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InitialSetupComponent {
  passwordVal = signal('');
  confirmPasswordVal = signal('');

  errorMessage = signal<string | null>(null);
  passwordInvalid = signal(false);
  confirmInvalid = signal(false);
  isSubmitting = signal(false);

  ngOnInit(): void {
    // Read server-reported error codes passed back via query params
    const params = new URLSearchParams(window.location.search);
    const serverError = params.get('error');

    if (serverError === 'weak') {
      this.showError(translate('setup.errors.weak'), true, false);
    } else if (serverError === 'mismatch') {
      this.showError(translate('setup.errors.mismatch'), false, true);
    }
  }

  onInput(field: 'password' | 'confirm', value: string): void {
    if (field === 'password') {
      this.passwordVal.set(value);
    } else {
      this.confirmPasswordVal.set(value);
    }
    this.clearError();
  }

  onSubmit(event: SubmitEvent): void {
    this.clearError();

    const pass = this.passwordVal();
    const confirm = this.confirmPasswordVal();

    if (pass.length < 8) {
      event.preventDefault();
      this.showError(translate('setup.errors.weak'), true, false);
      return;
    }

    if (pass !== confirm) {
      event.preventDefault();
      this.showError(translate('setup.errors.mismatch'), false, true);
      return;
    }

    this.isSubmitting.set(true);
  }

  private showError(msg: string, invalidPass: boolean, invalidConfirm: boolean): void {
    this.errorMessage.set(msg);
    this.passwordInvalid.set(invalidPass);
    this.confirmInvalid.set(invalidConfirm);
  }

  private clearError(): void {
    this.errorMessage.set(null);
    this.passwordInvalid.set(false);
    this.confirmInvalid.set(false);
  }
}
