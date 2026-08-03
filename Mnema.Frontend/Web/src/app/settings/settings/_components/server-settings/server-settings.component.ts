import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  effect,
  inject,
  OnInit,
  signal
} from '@angular/core';
import {UpdateServerSettings} from '../../../../_models/config';
import {FormGroup, ReactiveFormsModule} from "@angular/forms";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {SettingsService} from "../../../../_services/settings.service";
import {debounceTime, distinctUntilChanged, filter, skip, switchMap, tap} from "rxjs";
import {FormDefinition} from "@mnema/generic-form/form";
import {FormService} from "@mnema/_services/form.service";
import {GenericFormComponent} from "@mnema/generic-form/generic-form.component";
import {MetadataProvider} from "@mnema/features/monitored-series/metadata.service";
import {
  ProviderSettingsComponent
} from "@mnema/settings/settings/_components/provider-settings/provider-settings.component";
import {
  ConnectionSettingsComponent
} from "@mnema/settings/settings/_components/external-connection-settings/connection-settings.component";
import {
  DownloadClientSettingsComponent
} from "@mnema/settings/settings/_components/download-client/download-client-settings.component";
import {AuthKeysComponent} from "@mnema/settings/settings/_components/auth-keys/auth-keys.component";
import {MetadataProviderSettingsComponent} from "@mnema/settings/settings/_components/metadata-provider-settings/metadata-provider-settings.component";

@Component({
  selector: 'app-server-settings',
  imports: [
    ReactiveFormsModule,
    TranslocoDirective,
    GenericFormComponent,
    ProviderSettingsComponent,
    ConnectionSettingsComponent,
    DownloadClientSettingsComponent,
    AuthKeysComponent,
    MetadataProviderSettingsComponent
  ],
  templateUrl: './server-settings.component.html',
  styleUrl: './server-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ServerSettingsComponent implements OnInit {

  protected readonly settingsService = inject(SettingsService);
  private readonly formService = inject(FormService);

  protected serverSettingsFormDefinition = signal<FormDefinition | undefined>(undefined);
  protected serverSettingsForm = new FormGroup({});

  tab: 'general' | 'metadata-provider' | 'providers' | 'connections' | 'download-clients' | 'auth-keys' = 'general';

  ngOnInit() {
    this.formService.serverSettingsForm().pipe(
      tap(d => this.serverSettingsFormDefinition.set(d)),
    ).subscribe();

    this.serverSettingsForm.valueChanges.pipe(
      debounceTime(300),
      filter(() => this.serverSettingsForm.valid),
      distinctUntilChanged(),
      skip(1),
      switchMap(() => this.settingsService.updateConfig(this.serverSettingsForm.getRawValue() as UpdateServerSettings)),
    ).subscribe();
  }

  protected readonly translate = translate;
  protected readonly MetadataProvider = MetadataProvider;
}
