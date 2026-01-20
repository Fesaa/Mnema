import {ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, effect, inject, signal} from '@angular/core';
import {Config, MetadataProviderSettingsDto} from '../../../../_models/config';
import {FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators} from "@angular/forms";
import {ToastService} from "../../../../_services/toast.service";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {SettingsService} from "../../../../_services/settings.service";
import {SettingsItemComponent} from "../../../../shared/form/settings-item/settings-item.component";
import {takeUntilDestroyed, toObservable} from "@angular/core/rxjs-interop";
import {combineLatestWith, debounceTime, distinctUntilChanged, filter, map, merge, mergeWith, take, tap} from "rxjs";
import {FormDefinition} from "@mnema/generic-form/form";
import {FormService} from "@mnema/_services/form.service";
import {GenericFormComponent} from "@mnema/generic-form/generic-form.component";
import {MetadataProvider} from "@mnema/features/monitored-series/metadata.service";
import {GenericFormFactoryService} from "@mnema/generic-form/generic-form-factory.service";

@Component({
  selector: 'app-server-settings',
  imports: [
    ReactiveFormsModule,
    TranslocoDirective,
    SettingsItemComponent,
    GenericFormComponent
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
  private readonly genericFormFactory = inject(GenericFormFactoryService);
  private readonly formService = inject(FormService);
  private readonly destroyRef = inject(DestroyRef);

  config = this.settingsService.config;
  config$ = toObservable(this.config);

  settingsForm: FormGroup<{
    maxConcurrentImages: FormControl<number>
    maxConcurrentTorrents: FormControl<number>
    subscriptionRefreshHour: FormControl<number>;
  }> | undefined;

  metadataProvidersFormDefinition = signal<FormDefinition | null>(null);
  metadataProvidersForms = new Map<keyof typeof MetadataProvider, FormGroup>();

  tab: 'general' | 'metadata-provider' = 'general';
  metadataProviderTab: keyof typeof MetadataProvider = 'Hardcover';

  constructor() {
    this.config$.pipe(
      filter(c => !!c),
      combineLatestWith(this.formService.getMetadataProviderSettingsForm()),
      takeUntilDestroyed(this.destroyRef),
      take(1),
      tap(([c, f]) => this.setupForms(c, f))
    ).subscribe();
  }

  setupForms(config: Config, f: FormDefinition) {
    this.metadataProvidersFormDefinition.set(f);

    this.settingsForm = this.fb.group({
      maxConcurrentImages: this.fb.control(config.maxConcurrentImages, [Validators.required, Validators.min(1), Validators.max(5)]),
      maxConcurrentTorrents: this.fb.control(config.maxConcurrentTorrents, [Validators.required, Validators.min(1), Validators.max(10)]),
      subscriptionRefreshHour: this.fb.control(config.subscriptionRefreshHour),
    });

    for (let key in config.metadataProviderSettings) {
      this.metadataProvidersForms.set(key as any, new FormGroup({}));
    }

    this.cdRef.detectChanges();

    const allFormChanges$ = merge(
      this.settingsForm.valueChanges,
      ...Array.from(this.metadataProvidersForms.values()).map(form => form.valueChanges)
    );

    allFormChanges$.pipe(
      takeUntilDestroyed(this.destroyRef),
      distinctUntilChanged(),
      debounceTime(400),
      tap(() => this.save(false))
    ).subscribe();
  }

  getFormControl(path: string): FormControl | null {
    if (!this.settingsForm) return null;

    const control = this.settingsForm.get(path);
    return control instanceof FormControl ? control : null;
  }

  packData() {
    let config: any = this.settingsForm!.getRawValue();

    config.metadataProviderSettings = {};

    this.metadataProvidersForms.forEach((form, key) => {
      (config as Config).metadataProviderSettings[key] = this.genericFormFactory.adjustForNestedControls(
        form.getRawValue(),
        this.metadataProvidersFormDefinition()!.controls
      );
    });

    return config;
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

    const dto: Config = {
      ...this.packData(),
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
  protected readonly MetadataProvider = MetadataProvider;
}
