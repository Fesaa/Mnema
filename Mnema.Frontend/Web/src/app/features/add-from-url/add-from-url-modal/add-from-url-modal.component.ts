import {ChangeDetectionStrategy, Component, DestroyRef, inject, signal} from '@angular/core';
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {HttpErrorResponse} from "@angular/common/http";
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from "@angular/forms";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {TranslocoDirective} from "@jsverse/transloco";
import {catchError, EMPTY, finalize, tap} from "rxjs";
import {ContentService} from "@mnema/_services/content.service";

@Component({
  selector: 'app-add-from-url-modal',
  imports: [
    ReactiveFormsModule,
    TranslocoDirective,
  ],
  templateUrl: './add-from-url-modal.component.html',
  styleUrl: './add-from-url-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddFromUrlModalComponent {

  private readonly modal = inject(NgbActiveModal);
  private readonly contentService = inject(ContentService);
  private readonly destroyRef = inject(DestroyRef);

  form = new FormGroup({
    url: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
  });

  loading = signal(false);
  errorKey = signal<'not-found' | 'lookup-failed' | null>(null);

  lookup() {
    if (this.form.invalid || this.loading()) return;

    this.loading.set(true);
    this.errorKey.set(null);

    this.contentService.parseUrl(this.form.value.url!).pipe(
      takeUntilDestroyed(this.destroyRef),
      tap(parsed => this.modal.close(parsed)),
      catchError((err: HttpErrorResponse) => {
        this.errorKey.set(err.status === 404 ? 'not-found' : 'lookup-failed');
        return EMPTY;
      }),
      finalize(() => this.loading.set(false)),
    ).subscribe();
  }

  close() {
    this.modal.dismiss();
  }

}
