import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal
} from '@angular/core';
import {TranslocoDirective} from "@jsverse/transloco";
import {InUseProviders, Provider} from "@mnema/_models/page";
import {FormService} from "@mnema/_services/form.service";
import {catchError, forkJoin, of} from "rxjs";
import {
  ProviderSettingsService
} from "@mnema/settings/settings/_components/provider-settings/provider-settings.service";
import {MetadataBag} from "@mnema/_models/search";
import {ProviderNamePipe} from "@mnema/_pipes/provider-name.pipe";
import {FormDefinition} from "@mnema/generic-form/form";
import {FormGroup} from "@angular/forms";
import {GenericFormComponent} from "@mnema/generic-form/generic-form.component";
import {GenericFormFactoryService} from "@mnema/generic-form/generic-form-factory.service";
import {ItemListComponent} from "@mnema/shared/_component/item-list/item-list.component";
import {LoadingSpinnerComponent} from "@mnema/shared/_component/loading-spinner/loading-spinner.component";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";

@Component({
  selector: 'app-provider-settings',
  imports: [
    TranslocoDirective,
    ProviderNamePipe,
    GenericFormComponent,
    ItemListComponent,
    LoadingSpinnerComponent,
  ],
  templateUrl: './provider-settings.component.html',
  styleUrl: './provider-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProviderSettingsComponent {

  private readonly formService = inject(FormService);
  private readonly providerSettingsService = inject(ProviderSettingsService);
  private readonly genericFormFactoryService = inject(GenericFormFactoryService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly InUseProviders = InUseProviders;

  expandedProvider = signal<Provider | null>(null);
  formDefinition = signal<FormDefinition | undefined>(undefined);
  initialValue = signal<{metadata: MetadataBag} | undefined>(undefined);
  formGroup = signal<FormGroup | null>(null);
  loading = signal(false);

  protected trackBy(_: number, provider: Provider) {
    return provider + '';
  }

  protected toggle(provider: Provider) {
    if (this.expandedProvider() === provider) {
      this.collapse();
      return;
    }

    this.expandedProvider.set(provider);
    this.formDefinition.set(undefined);
    this.initialValue.set(undefined);
    this.formGroup.set(new FormGroup({}));
    this.loading.set(true);

    forkJoin([
      this.formService.getProviderSettingsForm(provider),
      this.providerSettingsService.get(provider),
    ]).pipe(
      takeUntilDestroyed(this.destroyRef),
      catchError(() => {
        this.collapse();
        return of(null);
      }),
    ).subscribe(result => {
      if (!result) return;

      const [form, settings] = result;
      this.formDefinition.set(form);
      this.initialValue.set({metadata: settings});
      this.loading.set(false);
    });
  }

  protected collapse() {
    this.expandedProvider.set(null);
    this.formDefinition.set(undefined);
    this.initialValue.set(undefined);
    this.formGroup.set(null);
    this.loading.set(false);
  }

  protected save(provider: Provider) {
    const group = this.formGroup();
    const initial = this.initialValue();
    if (!group || !initial || !group.valid) return;

    const value = {
      ...initial,
      ...this.genericFormFactoryService.adjustForGenericMetadata(group.getRawValue()),
    } as {metadata: MetadataBag};

    this.providerSettingsService.update(provider, value.metadata).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(() => this.collapse());
  }
}
