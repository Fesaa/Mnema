import {ChangeDetectorRef, Component, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {FormGroup, ReactiveFormsModule} from '@angular/forms';
import {PreferencesService} from '../../../../_services/preferences.service';
import {
  KavitaMetadataPreferences,
  Preferences
} from '../../../../_models/preferences';
import {TranslocoDirective} from '@jsverse/transloco';
import {debounceTime, distinctUntilChanged, filter, skip, switchMap, tap} from 'rxjs';
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {FormService} from "@mnema/_services/form.service";
import {FormDefinition} from "@mnema/generic-form/form";
import {GenericFormComponent} from "@mnema/generic-form/generic-form.component";
import {
  ConnectionService,
  ConnectionType
} from "@mnema/settings/settings/_components/external-connection-settings/connection.service";
import {UtilityService} from "@mnema/_services/utility.service";

@Component({
  selector: 'app-preference-settings',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslocoDirective,
    GenericFormComponent
  ],
  templateUrl: './preference-settings.component.html',
  styleUrl: './preference-settings.component.scss'
})
export class PreferenceSettingsComponent implements OnInit {

  private readonly destroyRef = inject(DestroyRef);
  private readonly preferencesService = inject(PreferencesService);
  private readonly formService = inject(FormService);
  private readonly utilityService = inject(UtilityService);

  preferences = signal<Preferences | undefined>(undefined);
  preferencesFormDefinition = signal<FormDefinition | undefined>(undefined);

  preferencesForm!: FormGroup;
  importMode = signal(false);

  ngOnInit(): void {
    this.preferencesService.get().subscribe((preferences: Preferences) => {
      this.preferences.set(preferences);
    });

    this.formService.preferencesForm().pipe(
      tap(d => this.preferencesFormDefinition.set(d)),
    ).subscribe();

    this.createFormGroup();
  }

  private createFormGroup() {
    this.preferencesForm = new FormGroup({});
    this.preferencesForm.valueChanges
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        debounceTime(300),
        distinctUntilChanged(),
        filter(() => this.preferencesForm.valid),
        skip(1), // First set by generic form component
        filter(() => !this.importMode()), // Import needs manual save
        switchMap(() => this.preferencesService.save(this.preferencesForm.getRawValue() as Preferences)),
      )
      .subscribe();
  }

  protected save() {
    this.preferencesService.save(this.preferencesForm.getRawValue() as Preferences).pipe(
      tap(() => this.importMode.set(false))
    ).subscribe();
  }

  protected export() {
    let id = 1;

    const preferences = this.preferencesForm.getRawValue() as Preferences;
    const kavitaPreferences: KavitaMetadataPreferences = {
      ageRatingMappings: Object.fromEntries(
        preferences.ageRatingMappings.map(item => [item.tag, item.ageRating])
      ),
      fieldMappings: preferences.metadataFieldMappings.map(m => ({
        id: id++,
        ...m,
      })),
      blacklist: preferences.blackListedTags,
      whitelist: preferences.whiteListedTags
    }

    this.utilityService.downloadObjectAsJson(kavitaPreferences, 'preferences.json');
  }

  protected import() {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json,application/json';

    input.onchange = (event: Event) => {
      const target = event.target as HTMLInputElement;
      const file = target.files?.[0];

      if (!file) {
        return;
      }

      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        try {
          const kavitaPreferences: KavitaMetadataPreferences = JSON.parse(e.target?.result as string);

          const importedPreferences: Preferences = {
            ...this.preferences(),
            ageRatingMappings: Object.entries(kavitaPreferences.ageRatingMappings ?? {}).map(
              ([tag, ageRating]) => ({ tag, ageRating: ageRating as any })
            ),
            metadataFieldMappings: kavitaPreferences.fieldMappings,
            blackListedTags: kavitaPreferences.blacklist,
            whiteListedTags: kavitaPreferences.whitelist
          } as Preferences;

          console.log('Imported preferences:', importedPreferences);

          this.importMode.set(true);
          this.preferences.set(undefined);
          this.createFormGroup();

          setTimeout(() => {
            this.preferences.set(importedPreferences);
          }, 100);
        } catch (error) {
          console.error('Error parsing imported JSON preferences:', error);
        }
      };

      reader.readAsText(file);
    };

    input.click();
  }

}
