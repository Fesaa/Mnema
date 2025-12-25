import {Component, computed, effect, input, output, signal} from '@angular/core';
import {FormBuilder, FormGroup, ReactiveFormsModule} from '@angular/forms';

import {SearchRequest} from "../../../_models/search";
import {Modifier, ModifierType, ModifierValue, Provider} from "../../../_models/page";
import {TranslocoDirective} from "@jsverse/transloco";
import {TypeaheadComponent, TypeaheadSettings} from "../../../type-ahead/typeahead.component";
import {of} from "rxjs";

@Component({
  selector: 'app-search-form',
  standalone: true,
  imports: [ReactiveFormsModule, TranslocoDirective, TypeaheadComponent],
  templateUrl: './search-form.component.html',
  styleUrls: ['./search-form.component.scss']
})
export class SearchFormComponent {

  title = input.required<string>();
  provider = input.required<Provider>();
  modifiers = input<Modifier[]>([]);
  loading = input<boolean>(false);

  hasModifiers = computed(() => this.modifiers().length > 0);

  searchSubmitted = output<SearchRequest>();
  modifierSelections = signal<{ [key: string]: string[] }>({});

  searchForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.searchForm = this.fb.group({query: ['']});

    effect(() => {
      this.searchForm.get('query')?.setValue('');
      this.setDefaultValues();
    });
  }

  private setDefaultValues(): void {
    const currentModifiers = this.modifiers();
    const defaultSelections: { [key: string]: string[] } = {};

    currentModifiers.forEach(modifier => {
      const defaults = this.getDefaultValues(modifier);
      if (!defaults) {
        defaultSelections[modifier.key] = [];
        return;
      }

      defaultSelections[modifier.key] = Array.isArray(defaults) ? defaults.map(mv => mv.key) : [defaults.key];
    });

    this.modifierSelections.set(defaultSelections);
  }

  private getDefaultValues(modifier: Modifier): ModifierValue[] | ModifierValue | undefined {
    const defaults = modifier.values
      .filter(value => value.default);

    if (defaults.length === 0) return undefined;

    if (modifier.type === ModifierType.DROPDOWN) {
      return defaults[0];
    }

    return defaults;
  }

  constructTypeaheadSettings(mod: Modifier): TypeaheadSettings<ModifierValue> {
    const settings = new TypeaheadSettings<ModifierValue>();
    settings.id = mod.key
    settings.multiple = mod.type === ModifierType.MULTI;
    settings.minCharacters = 0;

    settings.fetchFn = (f) => {
      if (mod.type === ModifierType.DROPDOWN) return of(mod.values);

      const filtered = mod.values
        .filter(v => v.value.toLowerCase().includes(f.toLowerCase()));

      return of(filtered);
    }

    const defaults = this.getDefaultValues(mod);
    if (defaults) {
      settings.savedData = defaults;
    }

    settings.trackByIdentityFn = (idx, mv) =>  `${mv.key}`;
    settings.selectionCompareFn = (mv1, mv2) => mv1.key === mv2.key;

    return settings;
  }

  onModifierSwitchChange(mod: Modifier, event: Event) {
    const selected = (event.target as HTMLInputElement).checked;
    const value: ModifierValue = {
      key: selected ? 'true' : 'false',
      value: selected ? 'true' : 'false',
      default: false,
    }
    this.onModifierSelection(mod, value);
  }

  onModifierSelection(mod: Modifier, event: ModifierValue[] | ModifierValue) {
    this.modifierSelections.update(s => {
      s[mod.key] = Array.isArray(event) ? event.map(mv => mv.key) : [event.key];
      return s;
    })
  }

  onSubmit(): void {
    if (!this.searchForm.valid) {
      return;
    }

    const formValue = this.searchForm.value;
    const modifierSelections = this.modifierSelections();
    const modifiersToSend: { [key: string]: string[] } = {};

    this.modifiers().forEach(modifier => {
      const selections = modifierSelections[modifier.key] || [];
      if (selections.length > 0) {
        modifiersToSend[modifier.key] = modifier.type === ModifierType.MULTI ? selections : [selections[0]];
      }
    });

    const searchRequest: SearchRequest = {
      provider: this.provider(),
      query: formValue.query,
      modifiers: Object.keys(modifiersToSend).length > 0 ? modifiersToSend :{}
    };

    this.searchSubmitted.emit(searchRequest);
  }

  trackModifier = (index: number, modifier: Modifier) => {
    return `${this.title()}_${index}_${modifier.title}`
  };
  protected readonly ModifierType = ModifierType;
}
