import {Component, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {PreferencesService} from '../../../../_services/preferences.service';
import {
  AgeRatingMap,
  ComicInfoAgeRating,
  ComicInfoAgeRatings,
  CoverFallbackMethods,
  ImageFormats,
  Preferences,
  TagMap
} from '../../../../_models/preferences';
import {ToastService} from '../../../../_services/toast.service';
import {TranslocoDirective} from '@jsverse/transloco';
import {debounceTime, distinctUntilChanged, filter, map, switchMap} from 'rxjs';
import {SettingsItemComponent} from "../../../../shared/form/settings-item/settings-item.component";
import {TagBadgeComponent} from "../../../../shared/_component/tag-badge/tag-badge.component";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {CoverFallbackPipe} from "../../../../_pipes/cover-fallback.pipe";
import {SettingsSwitchComponent} from "../../../../shared/form/settings-switch/settings-switch.component";
import {SafeHtmlPipe} from "../../../../_pipes/safe-html-pipe";
import {NgbNav, NgbNavContent, NgbNavItem, NgbNavLink, NgbNavOutlet} from "@ng-bootstrap/ng-bootstrap";
import {ImageFormatPipe} from "../../../../_pipes/image-format.pipe";

@Component({
  selector: 'app-preference-settings',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslocoDirective,
    SettingsItemComponent,
    TagBadgeComponent,
    CoverFallbackPipe,
    SafeHtmlPipe,
    NgbNav,
    NgbNavItem,
    NgbNavOutlet,
    NgbNavContent,
    NgbNavLink,
    ImageFormatPipe,
    SettingsSwitchComponent
  ],
  templateUrl: './preference-settings.component.html',
  styleUrl: './preference-settings.component.scss'
})
export class PreferenceSettingsComponent implements OnInit {

  private readonly destroyRef = inject(DestroyRef);
  private readonly preferencesService = inject(PreferencesService);
  private readonly toastService = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  preferences = signal<Preferences | undefined>(undefined);

  preferencesForm!: FormGroup;
  activeId = 'general'

  protected readonly CoverFallbackMethods = CoverFallbackMethods;

  ngOnInit(): void {
    this.preferencesService.get().subscribe((preferences: Preferences) => {
      this.preferences.set(preferences);

      this.preferencesForm = this.fb.group({
        imageFormat: new FormControl(preferences.imageFormat),
        coverFallbackMethod: new FormControl(preferences.coverFallbackMethod),
        pinSubscriptionTitles: new FormControl(preferences.pinSubscriptionTitles),
        blackList: new FormControl(preferences.blackListedTags.join(',')),
        whiteList: new FormControl(preferences.whiteListedTags.join(',')),
        genreList: new FormControl(preferences.convertToGenreList.join(',')),
        ageRatingMappings: new FormArray(preferences.ageRatingMappings.map(agm => this.ageRateMappingToFormGroup(agm))),
        tagMappings: new FormArray(preferences.tagMappings.map(agm => this.tagMappingToFormGroup(agm))),
      });

      this.preferencesForm.valueChanges
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          debounceTime(300),
          distinctUntilChanged(),
          filter(() => this.preferencesForm.valid),
          map(() => this.packData()),
          switchMap(data => this.preferencesService.save(data)),
        )
        .subscribe({
          error: err => this.toastService.genericError(err.error.message)
        });
    });
  }

  get ageRatingMappingArray(): FormArray<FormGroup> {
    return this.preferencesForm.get('ageRatingMappings') as FormArray<FormGroup>;
  }

  get tagMappingsArray(): FormArray<FormGroup> {
    return this.preferencesForm.get('tagMappings') as FormArray<FormGroup>;
  }

  deleteTagMapping(idx: number) {
    this.tagMappingsArray.removeAt(idx);
  }

  addTagMapping() {
    this.tagMappingsArray.push(this.tagMappingToFormGroup({
      destinationTag: '',
      originTag: ''
    }));
  }

  private tagMappingToFormGroup(tm: TagMap) {
    return new FormGroup({
      originTag: new FormControl(tm.originTag, Validators.required),
      destinationTag: new FormControl(tm.destinationTag, Validators.required),
    })
  }

  deleteAgeRatingMapping(idx: number) {
    this.ageRatingMappingArray.removeAt(idx);
  }

  addAgeRateMapping() {
    this.ageRatingMappingArray.push(this.ageRateMappingToFormGroup({
      tag: '',
      ageRating: ComicInfoAgeRating.Pending,
    }));
  }

  private ageRateMappingToFormGroup(agm: AgeRatingMap) {
    return new FormGroup({
      tag: new FormControl(agm.tag, [Validators.required]),
      ageRating: new FormControl(agm.ageRating, [Validators.required]),
    })
  }

  packData(): Preferences {
    const preferences = this.preferences();
    const formValue = this.preferencesForm.value;

    return {
      ...preferences,
      ...formValue,
      coverFallbackMethod: parseInt(formValue.coverFallbackMethod),
      blackListedTags: (formValue.blackList as string)
        .split(',').map((item: string) => item.trim())
        .filter((t: string) => t.length > 0),
      whiteListedTags: (formValue.whiteList as string)
        .split(',').map((item: string) => item.trim())
        .filter((t: string) => t.length > 0),
      convertToGenreList: (formValue.genreList as string)
        .split(',').map((item: string) => item.trim())
        .filter((t: string) => t.length > 0),
    };
  }

  breakString(s: string) {
    if (s) {
      return s.split(',').filter(s => s.length > 0);
    }

    return [];
  }

  protected readonly ComicInfoAgeRatings = ComicInfoAgeRatings;
  protected readonly ImageFormats = ImageFormats;
}
