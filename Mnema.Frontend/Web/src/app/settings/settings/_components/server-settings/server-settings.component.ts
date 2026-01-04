import {ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, effect, inject} from '@angular/core';
import {Config} from '../../../../_models/config';
import {
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators
} from "@angular/forms";
import {ToastService} from "../../../../_services/toast.service";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {SettingsService} from "../../../../_services/settings.service";
import {SettingsItemComponent} from "../../../../shared/form/settings-item/settings-item.component";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {debounceTime, distinctUntilChanged, map, tap} from "rxjs";

@Component({
  selector: 'app-server-settings',
  imports: [
    ReactiveFormsModule,
    TranslocoDirective,
    SettingsItemComponent
  ],
  templateUrl: './server-settings.component.html',
  styleUrl: './server-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ServerSettingsComponent {

  private readonly settingsService = inject(SettingsService);
  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly cdRef = inject(ChangeDetectorRef);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  config = this.settingsService.config;

  settingsForm: FormGroup<{
    maxConcurrentImages: FormControl<number>
    maxConcurrentTorrents: FormControl<number>
    subscriptionRefreshHour: FormControl<number>;
  }> | undefined;

  constructor() {
    effect(() => {
      const config = this.settingsService.config();
      if (config == undefined) return

      this.settingsForm = this.fb.group({
        maxConcurrentImages: this.fb.control(config.maxConcurrentImages, [Validators.required, Validators.min(1), Validators.max(5)]),
        maxConcurrentTorrents: this.fb.control(config.maxConcurrentTorrents, [Validators.required, Validators.min(1), Validators.max(10)]),
        subscriptionRefreshHour: this.fb.control(config.subscriptionRefreshHour),
      });
      this.cdRef.detectChanges();

      this.settingsForm.valueChanges.pipe(
        takeUntilDestroyed(this.destroyRef),
        distinctUntilChanged(),
        debounceTime(400),
        map(() => this.settingsForm?.getRawValue()),
        tap(() => this.save(false))
      ).subscribe();
    });
  }

  getFormControl(path: string): FormControl | null {
    if (!this.settingsForm) return null;

    const control = this.settingsForm.get(path);
    return control instanceof FormControl ? control : null;
  }

  save(toastOnSuccess: boolean = true) {
    if (!this.settingsForm) {
      return;
    }

    const errors = this.errors();
    if (errors > 0) {
      this.toastService.errorLoco("settings.server.toasts.cant-submit", {}, {amount: errors});
      return;
    }

    if (!this.settingsForm.dirty) {
      this.toastService.warningLoco("shared.toasts.no-changes")
      return;
    }

    const dto: Config = {
      ...this.settingsForm.getRawValue(),
    } as Config;
    dto.maxConcurrentImages = parseInt(String(dto.maxConcurrentImages))
    dto.maxConcurrentTorrents = parseInt(String(dto.maxConcurrentTorrents))

    this.settingsService.updateConfig(dto).subscribe({
      next: () => {
        if (toastOnSuccess) {
          this.toastService.successLoco("settings.server.toasts.save.success");
        }
      },
      error: (error) => {
        console.error(error);
        this.toastService.genericError(error.error.message);
      }
    });
  }

  private errors() {
    let count = 0;
    Object.keys(this.settingsForm!.controls).forEach(key => {
      const controlErrors = this.settingsForm!.get(key)?.errors;
      if (controlErrors) {
        console.log(controlErrors);
        count += Object.keys(controlErrors).length;
      }
    });

    return count
  }

  protected readonly translate = translate;
}
