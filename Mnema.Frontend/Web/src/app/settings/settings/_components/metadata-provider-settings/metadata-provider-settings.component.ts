import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal
} from '@angular/core';
import {FormService} from "@mnema/_services/form.service";
import {MetadataProvider} from "@mnema/features/monitored-series/metadata.service";
import {catchError, forkJoin, of, tap} from "rxjs";
import {TranslocoDirective} from "@jsverse/transloco";
import {MetadataProviderPipe} from "@mnema/_pipes/metadata-provider.pipe";
import {SettingsService} from "@mnema/_services/settings.service";
import {MetadataProviderSettings} from "@mnema/_models/metadata-provider-settings";
import {CdkDragDrop, CdkDragHandle, moveItemInArray} from "@angular/cdk/drag-drop";
import {FormDefinition} from "@mnema/generic-form/form";
import {FormGroup} from "@angular/forms";
import {GenericFormComponent} from "@mnema/generic-form/generic-form.component";
import {GenericFormFactoryService} from "@mnema/generic-form/generic-form-factory.service";
import {ItemListComponent} from "@mnema/shared/_component/item-list/item-list.component";
import {LoadingSpinnerComponent} from "@mnema/shared/_component/loading-spinner/loading-spinner.component";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";

@Component({
  selector: 'app-metadata-provider-settings',
  imports: [
    TranslocoDirective,
    MetadataProviderPipe,
    CdkDragHandle,
    GenericFormComponent,
    ItemListComponent,
    LoadingSpinnerComponent,
  ],
  templateUrl: './metadata-provider-settings.component.html',
  styleUrl: './metadata-provider-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MetadataProviderSettingsComponent implements OnInit {

  private readonly formService = inject(FormService);
  private readonly settingsService = inject(SettingsService);
  private readonly genericFormFactoryService = inject(GenericFormFactoryService);
  private readonly destroyRef = inject(DestroyRef);

  sortedMetadataProviders = signal<MetadataProvider[]>([]);
  expandedProvider = signal<MetadataProvider | null>(null);
  formDefinition = signal<FormDefinition | undefined>(undefined);
  initialValue = signal<MetadataProviderSettings | undefined>(undefined);
  formGroup = signal<FormGroup | null>(null);
  loading = signal(false);

  ngOnInit() {
    this.settingsService.getMetadataProviderOrder().pipe(
      takeUntilDestroyed(this.destroyRef),
      tap(order => this.sortedMetadataProviders.set(order))
    ).subscribe();
  }

  protected toggle(metadataProvider: MetadataProvider) {
    if (this.expandedProvider() === metadataProvider) {
      this.collapse();
      return;
    }

    this.expandedProvider.set(metadataProvider);
    this.formDefinition.set(undefined);
    this.initialValue.set(undefined);
    this.formGroup.set(new FormGroup({}));
    this.loading.set(true);

    forkJoin([
      this.formService.getMetadataProviderSettingsForm(metadataProvider),
      this.settingsService.getMetadataSettings(metadataProvider),
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
      this.initialValue.set(settings);
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

  protected save() {
    const group = this.formGroup();
    const initial = this.initialValue();
    if (!group || !initial || !group.valid) return;

    const settings = {
      ...initial,
      ...this.genericFormFactoryService.adjustForGenericMetadata(group.getRawValue()),
    } as MetadataProviderSettings;

    this.settingsService.saveMetadataSettings(settings).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(() => this.collapse());
  }

  trackBy(idx: number, metadataProvider: MetadataProvider) {
    return metadataProvider + '';
  }

  protected drop($event: CdkDragDrop<MetadataProvider[]>) {
    const metadataProviders = [...this.sortedMetadataProviders()];
    const copy = [...metadataProviders];

    moveItemInArray(metadataProviders, $event.previousIndex, $event.currentIndex);
    this.sortedMetadataProviders.set(metadataProviders);

    this.settingsService.sortMetadataProviders(metadataProviders).pipe(
      catchError(() => {
        this.sortedMetadataProviders.set(copy);
        return of(null);
      })
    ).subscribe();
  }
}
