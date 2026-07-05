import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {AllProviders, Provider} from "@mnema/_models/page";
import {FormService} from "@mnema/_services/form.service";
import {forkJoin, switchMap} from "rxjs";
import {ModalService} from "@mnema/_services/modal.service";
import {GenericFormModalComponent} from "@mnema/generic-form/generic-form-modal/generic-form-modal.component";
import {DefaultModalOptions} from "@mnema/_models/default-modal-options";
import {
  ProviderSettingsService
} from "@mnema/settings/settings/_components/provider-settings/provider-settings.service";
import {MetadataBag} from "@mnema/_models/search";
import {TableComponent} from "@mnema/shared/_component/table/table.component";
import {ProviderNamePipe} from "@mnema/_pipes/provider-name.pipe";

@Component({
  selector: 'app-provider-settings',
  imports: [
    TranslocoDirective,
    TableComponent,
    ProviderNamePipe
  ],
  templateUrl: './provider-settings.component.html',
  styleUrl: './provider-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProviderSettingsComponent {

  private readonly formService = inject(FormService);
  private readonly modalService = inject(ModalService);
  private readonly providerSettingsService = inject(ProviderSettingsService);
  private readonly providerPipe = new ProviderNamePipe();

  protected trackBy(_: number, provider: Provider) {
    return provider + '';
  }

  protected editProviderSettings(provider: Provider) {
    forkJoin([
      this.formService.getProviderSettingsForm(provider),
      this.providerSettingsService.get(provider)
    ]).pipe(
      switchMap(([form, settings]) => {
        const [modal, component] = this.modalService.open(GenericFormModalComponent, DefaultModalOptions);

        component.double.set(true);
        component.formDefinition.set(form);
        component.initialValue.set({metadata: settings});
        component.translationKey.set('settings.provider-settings.edit');
        component.title.set(translate('settings.provider-settings.edit.title', {provider: this.providerPipe.transform(provider)}));

        return this.modalService.onClose$<{metadata: MetadataBag}>(modal);
      }),
      switchMap(holder => this.providerSettingsService.update(provider, holder.metadata)),
    ).subscribe();
  }

  protected readonly AllProviders = AllProviders;
}
