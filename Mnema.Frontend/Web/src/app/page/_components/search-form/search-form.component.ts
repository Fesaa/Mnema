import {Component, computed, effect, input, output, signal, untracked, viewChild} from '@angular/core';
import {FormBuilder, FormGroup, ReactiveFormsModule} from '@angular/forms';

import {MetadataBag, SearchRequest} from "@mnema/_models/search";
import {Page} from "@mnema/_models/page";
import {TranslocoDirective} from "@jsverse/transloco";
import {TypeaheadComponent, TypeaheadSettings} from "@mnema/type-ahead/typeahead.component";
import {of} from "rxjs";
import {FormControlDefinition, FormControlOption, FormType} from "@mnema/generic-form/form";

@Component({
  selector: 'app-search-form',
  standalone: true,
  imports: [ReactiveFormsModule, TranslocoDirective, TypeaheadComponent],
  templateUrl: './search-form.component.html',
  styleUrls: ['./search-form.component.scss']
})
export class SearchFormComponent {

  page = input.required<Page>();
  onlyModifiers = input(false);

  title = computed(() => this.page().title);
  provider = computed(() => this.page().provider);
  modifiers = computed(() => this.page().modifiers ?? []);
  loading = input<boolean>(false);

  hasModifiers = computed(() => this.modifiers().length > 0);

  searchSubmitted = output<SearchRequest>();

  modifierSelections = signal<{ [key: string]: string[] }>({});

  searchForm: FormGroup;

  settings = signal<Record<string, TypeaheadSettings<FormControlOption>>>({});

  constructor(private fb: FormBuilder) {
    this.searchForm = this.fb.group({query: ['']});

    effect(() => {
      this.searchForm.get('query')?.setValue('');
      this.setDefaultValues();
      this.loadTypeaheadSettings();
    });
  }

  private loadTypeaheadSettings() {
    const settings: Record<string, TypeaheadSettings<FormControlOption>> = {};
    this.modifiers().forEach(mod => {
      settings[mod.key] = this.constructTypeaheadSettings(mod);
    });
    this.settings.set(settings);
  }

  private setDefaultValues(): void {
    const currentModifiers = this.modifiers();
    const defaultSelections: { [key: string]: string[] } = {};

    currentModifiers.forEach(modifier => {
      const userDefault = untracked(this.page).defaultOptions[modifier.key];
      if (userDefault && userDefault.length > 0) {
        defaultSelections[modifier.key] = userDefault;
        return;
      }

      const defaults = this.getDefaultValues(modifier);
      if (!defaults) {
        defaultSelections[modifier.key] = [];
        return;
      }

      defaultSelections[modifier.key] = Array.isArray(defaults) ? defaults.map(mv => mv.value) : [defaults.value];
    });

    this.modifierSelections.set(defaultSelections);
  }

  private getDefaultValues(modifier: FormControlDefinition): FormControlOption[] | FormControlOption | undefined {
    const defaults = (modifier.options ?? [])
      .filter(value => value.default);

    if (defaults.length === 0) return undefined;

    if (modifier.fieldType === FormType.DropDown) {
      return defaults[0];
    }

    return defaults;
  }

  constructTypeaheadSettings(mod: FormControlDefinition): TypeaheadSettings<FormControlOption> {
    const settings = new TypeaheadSettings<FormControlOption>();
    settings.id = mod.key
    settings.multiple = mod.fieldType === FormType.MultiSelect;
    settings.minCharacters = (mod.options ?? []).length > 10 ? 1 : 0;

    settings.fetchFn = (f) => {
      if (mod.fieldType === FormType.DropDown) return of(mod.options ?? []);

      const filtered = (mod.options ?? [])
        .filter(v => v.key.toLowerCase().includes(f.toLowerCase())
          || v.value.toLowerCase().includes(f.toLowerCase()));

      return of(filtered);
    }

    const defaults = untracked(this.modifierSelections)[mod.key];
    if (defaults) {
      settings.savedData = (mod.options ?? []).filter(o => defaults.includes(o.value));
    }

    settings.trackByIdentityFn = (idx, mv) =>  `${mv.key}`;
    settings.selectionCompareFn = (mv1, mv2) => mv1.key === mv2.key;

    return settings;
  }

  onModifierSwitchChange(mod: FormControlDefinition, event: Event) {
    const selected = (event.target as HTMLInputElement).checked;
    const value: FormControlOption = {
      key: selected ? 'true' : 'false',
      value: selected ? 'true' : 'false',
      default: false,
    }
    this.onModifierSelection(mod, value);
  }

  onModifierSelection(mod: FormControlDefinition, event: FormControlOption[] | FormControlOption) {
    this.modifierSelections.update(s => ({
      ...s,
      [mod.key]: Array.isArray(event) ? event.map(mv => mv.value) : [event.value]
    }));
  }

  onDropdownChange(mod: FormControlDefinition, event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    const option = (mod.options ?? []).find(o => o.value === value);
    console.log(mod.key, value, option);
    if (option) {
      this.onModifierSelection(mod, option);
    }
  }

  isSwitchChecked(mod: FormControlDefinition): boolean {
    return this.modifierSelections()[mod.key]?.[0] === 'true';
  }

  getDropdownValue(mod: FormControlDefinition): string {
    return this.modifierSelections()[mod.key]?.[0] ?? '';
  }

  onSubmit(): void {
    if (!this.searchForm.valid) {
      return;
    }

    const formValue = this.searchForm.value;


    const searchRequest: SearchRequest = {
      provider: this.provider(),
      query: formValue.query,
      modifiers: this.packModifiers(),
    };

    this.searchSubmitted.emit(searchRequest);
  }

  packModifiers(): MetadataBag {
    const modifierSelections = this.modifierSelections();
    const modifiersToSend: { [key: string]: string[] } = {};

    this.modifiers().forEach(modifier => {
      const selections = modifierSelections[modifier.key] || [];
      if (selections.length > 0) {
        modifiersToSend[modifier.key] = modifier.fieldType === FormType.MultiSelect ? selections : [selections[0]];
      }
    });

    return Object.keys(modifiersToSend).length > 0 ? modifiersToSend :{};
  }

  trackModifier = (index: number, modifier: FormControlDefinition) => {
    return `${this.title()}_${index}_${modifier.key}`
  };

  protected readonly FormType = FormType;
}
