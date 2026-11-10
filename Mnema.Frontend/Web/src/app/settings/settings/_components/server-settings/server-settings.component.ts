import {ChangeDetectionStrategy, Component, inject, OnInit, signal} from '@angular/core';
import {UpdateServerSettings} from '../../../../_models/config';
import {FormGroup} from "@angular/forms";
import {TranslocoDirective} from "@jsverse/transloco";
import {SettingsService} from "../../../../_services/settings.service";
import {debounceTime, distinctUntilChanged, filter, skip, switchMap, tap} from "rxjs";
import {FormDefinition} from "@mnema/generic-form/form";
import {FormService} from "@mnema/_services/form.service";
import {GenericFormComponent} from "@mnema/generic-form/generic-form.component";
import {AuthKeysComponent} from "@mnema/settings/settings/_components/auth-keys/auth-keys.component";

@Component({
  selector: 'app-server-settings',
  imports: [
    TranslocoDirective,
    GenericFormComponent,
    AuthKeysComponent,
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
}
