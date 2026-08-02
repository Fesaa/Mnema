import {Component, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {FormGroup, ReactiveFormsModule} from '@angular/forms';
import {PreferencesService} from '../../../../_services/preferences.service';
import {
  Preferences
} from '../../../../_models/preferences';
import {TranslocoDirective} from '@jsverse/transloco';
import {debounceTime, distinctUntilChanged, filter, skip, switchMap, tap} from 'rxjs';
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {FormService} from "@mnema/_services/form.service";
import {FormDefinition} from "@mnema/generic-form/form";
import {GenericFormComponent} from "@mnema/generic-form/generic-form.component";

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

  preferences = signal<Preferences | undefined>(undefined);
  preferencesFormDefinition = signal<FormDefinition | undefined>(undefined);

  preferencesForm = new FormGroup({});

  ngOnInit(): void {
    this.preferencesService.get().subscribe((preferences: Preferences) => {
      this.preferences.set(preferences);
    });

    this.formService.preferencesForm().pipe(
      tap(d => this.preferencesFormDefinition.set(d)),
    ).subscribe();

    this.preferencesForm.valueChanges
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        debounceTime(300),
        distinctUntilChanged(),
        filter(() => this.preferencesForm.valid),
        skip(1), // First set by generic form component
        switchMap(() => this.preferencesService.save(this.preferencesForm.getRawValue() as Preferences)),
      )
      .subscribe();
  }

}
