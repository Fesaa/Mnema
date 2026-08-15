import {ChangeDetectionStrategy, Component, inject, OnInit, signal} from '@angular/core';
import {FormService} from "@mnema/_services/form.service";
import {MetadataProvider} from "@mnema/features/monitored-series/metadata.service";
import {ModalService} from "@mnema/_services/modal.service";
import {catchError, of, switchMap, tap} from "rxjs";
import {GenericFormModalComponent} from "@mnema/generic-form/generic-form-modal/generic-form-modal.component";
import {DefaultModalOptions} from "@mnema/_models/default-modal-options";
import {TableComponent} from "@mnema/shared/_component/table/table.component";
import {TranslocoDirective} from "@jsverse/transloco";
import {MetadataProviderPipe} from "@mnema/_pipes/metadata-provider.pipe";
import {SettingsService} from "@mnema/_services/settings.service";
import {MetadataProviderSettings} from "@mnema/_models/metadata-provider-settings";
import {CdkDragDrop, CdkDragHandle, moveItemInArray} from "@angular/cdk/drag-drop";

@Component({
  selector: 'app-metadata-provider-settings',
  imports: [
    TableComponent,
    TranslocoDirective,
    MetadataProviderPipe,
    CdkDragHandle
  ],
  templateUrl: './metadata-provider-settings.component.html',
  styleUrl: './metadata-provider-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MetadataProviderSettingsComponent implements OnInit {

  private readonly formService = inject(FormService);
  private readonly modalService = inject(ModalService);
  private readonly settingsService = inject(SettingsService);

  sortedMetadataProviders = signal<MetadataProvider[]>([]);

  ngOnInit() {
    this.settingsService.getMetadataProviderOrder().pipe(
      tap(order => this.sortedMetadataProviders.set(order))
    ).subscribe();
  }

  protected edit(metadataProvider: MetadataProvider) {
    this.formService.getMetadataProviderSettingsForm(metadataProvider).pipe(
      switchMap(formDefinition => this.settingsService.getMetadataSettings(metadataProvider).pipe(
        switchMap(settings => {
          const [modal, component] = this.modalService.open(GenericFormModalComponent, DefaultModalOptions);
          component.formDefinition.set(formDefinition);
          component.initialValue.set(settings);
          component.translationKey.set('settings.metadata-provider');

          return this.modalService.onClose$<MetadataProviderSettings>(modal);
        }))),
      switchMap(settings => this.settingsService.saveMetadataSettings(settings))
    ).subscribe();
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
