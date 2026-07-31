import {ChangeDetectionStrategy, Component, OnInit} from '@angular/core';
import {TranslocoDirective} from "@jsverse/transloco";

@Component({
  selector: 'app-login',
  imports: [
    TranslocoDirective
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent implements OnInit {
  returnUrl = '';
  hasError = false;

  ngOnInit(): void {
    const params = new URLSearchParams(window.location.search);
    this.returnUrl = params.get('ReturnUrl') || params.get('returnUrl') || '';
    this.hasError = params.has('error');
  }

  onSubmit(event: SubmitEvent): void {
    const btn = (event.target as HTMLFormElement)?.querySelector('#submitBtn') as HTMLButtonElement;
    if (btn) {
      btn.disabled = true;
    }
  }
}
