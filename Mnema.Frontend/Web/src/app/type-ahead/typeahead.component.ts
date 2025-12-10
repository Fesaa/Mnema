import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ContentChild,
  DestroyRef,
  effect,
  ElementRef,
  EventEmitter,
  inject,
  input,
  OnInit,
  Output,
  signal,
  TemplateRef,
  ViewChild
} from '@angular/core';
import {catchError, debounceTime, distinctUntilChanged, Observable, of, startWith, Subject, switchMap} from 'rxjs';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {NgTemplateOutlet} from '@angular/common';
import {TranslocoDirective} from '@jsverse/transloco';

export type SelectionCompareFn<T> = (a: T, b: T) => boolean;

export class TypeaheadSettings<T>  {
  /**
   * How many ms between typing actions before pipeline to load data is triggered
   */
  debounce: number = 200;
  /**
   * Multiple options can be selected from dropdown. Will be rendered as tag badges.
   */
  multiple: boolean = false;
  /**
   * Id of the input element, for linking label elements (accessibility)
   */
  id: string = '';
  /**
   * Show a locked icon next to input and provide functionality around locking/unlocking a field
   */
  showLocked: boolean = false;
  /**
   * Data to preload the typeahead with on first load
   */
  savedData!: T[] | T;
  /**
   * Must be defined when addIfNonExisting is true. Used to ensure no duplicates exist when adding.
   */
  compareFnForAdd!: ((optionList: T[], filter: string)  => T[]);
  /**
   * Function which is used for comparing objects when keeping track of state.
   * Useful over shallow equal when you have image urls that have random numbers on them.
   */
  selectionCompareFn?: SelectionCompareFn<T>;
  /**
   * Function to fetch the data from the server. If data is maintained in memory, wrap in an observable.
   */
  fetchFn!: (filter: string) => Observable<T[]>;
  /**
   * Minimum number of characters needed to type to trigger the fetch pipeline
   */
  minCharacters: number = 1;
  /**
   * Optional form Control to tie model to.
   */
  formControl?: FormControl;
  /**
   * If true, typeahead will remove already selected items from fetchFn results. Only appies when multiple=true
   */
  unique: boolean = true;
  /**
   * If true, will fire an event for newItemAdded and will prompt the user to add form model to the list of selected items
   */
  addIfNonExisting: boolean = false;
  /**
   * Required for addIfNonExisting to transform the text from model into the item
   */
  addTransformFn!: (text: string) => T;
  /**
   * An optional, but recommended trackby identity function to help Angular render the list better
   */
  trackByIdentityFn?: (index: number, value: T) => string;
}

@Component({
  selector: 'app-type-ahead',
  imports: [
    ReactiveFormsModule,
    NgTemplateOutlet,
    TranslocoDirective
  ],
  templateUrl: './typeahead.component.html',
  styleUrl: './typeahead.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TypeaheadComponent<T> implements OnInit {

  private destroyRef = inject(DestroyRef);

  settings = input.required<TypeaheadSettings<T>>();
  locked = input(false);
  disabled = input(false);

  @Output() selectedData = new EventEmitter<T[] | T>();
  @Output() newItemAdded = new EventEmitter<T[] | T>();
  @Output() lockedChange = new EventEmitter<boolean>();

  @ViewChild('input') inputElem!: ElementRef<HTMLInputElement>;
  @ContentChild('label') labelTemplate?: TemplateRef<any>;
  @ContentChild('optionItem') optionTemplate?: TemplateRef<any>;
  @ContentChild('badgeItem') badgeTemplate?: TemplateRef<any>;

  // Signals
  hasFocus = signal(false);
  showAddItem = signal(false);
  selectedItems = signal<T[]>([]);
  filteredOptions = signal<T[]>([]);
  highlightedIndex = signal(-1);
  trackByIdentityFn = computed(() => {
    const settings = this.settings();
    if (settings.trackByIdentityFn) {
      return settings.trackByIdentityFn;
    }
    return (idx: number, t: T) => `${idx}`;
  })

  // Form Control
  searchControl = new FormControl('');

  // Subjects
  private searchSubject = new Subject<string>();

  // Computed signals
  showDropdown = computed(() =>
    this.hasFocus() &&
    (this.filteredOptions().length > 0 ||
      (this.searchControl.value && this.searchControl.value.length >= this.settings().minCharacters && this.settings().addIfNonExisting))
  );

  hasSelections = computed(() => this.selectedItems().length > 0);

  showClearButton = computed(() =>
    !this.disabled() &&
    !this.locked() &&
    (this.hasSelections() || (this.searchControl.value && this.searchControl.value.length > 0))
  );

  constructor() {
    effect(() => {
      const settings = this.settings();
      if (!settings.fetchFn) return;

      this.searchSubject
        .pipe(
          debounceTime(settings.debounce),
          distinctUntilChanged(),
          switchMap(term => {
            if ((!term && settings.minCharacters !== 0 )|| term.length < settings.minCharacters) {
              return of([]);
            }

            return settings.fetchFn(term).pipe(
              catchError(error => {
                console.error('Typeahead search error:', error);
                return of([] as T[]);
              }),
              startWith([] as T[])
            );
          }),
          takeUntilDestroyed(this.destroyRef)
        )
        .subscribe(results => {
          let filteredResults = results;

          // Remove already selected items if unique is true and multiple is enabled
          if (settings.unique && settings.multiple && this.selectedItems().length > 0) {
            filteredResults = results.filter(result => !this.isSelected(result));
          }

          this.filteredOptions.set(filteredResults);
          this.highlightedIndex.set(filteredResults.length > 0 ? 0 : -1);

          // Check if we should show add item option
          if (settings.addIfNonExisting && this.searchControl.value) {
            const existsInResults = settings.compareFnForAdd(results, this.searchControl.value).length > 0;
            this.showAddItem.set(!existsInResults);
          }
        });
    });

    this.searchControl.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(value => {
        this.searchSubject.next(value || '');
      });
  }

  ngOnInit(): void {
    const settings = this.settings();

    if (settings.savedData) {
      if (settings.multiple && Array.isArray(settings.savedData)) {
        this.selectedItems.set([...settings.savedData]);
      } else if (!settings.multiple && !Array.isArray(settings.savedData)) {
        this.selectedItems.set([settings.savedData]);
        this.searchControl.setValue(this.getDisplayText(settings.savedData));
      }
    }

    // Setup form control if provided
    if (settings.formControl) {
      settings.formControl.valueChanges
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(value => {
          if (value !== this.getCurrentValue()) {
            if (settings.multiple && Array.isArray(value)) {
              this.selectedItems.set([...value]);
            } else if (!settings.multiple && value) {
              this.selectedItems.set([value]);
            } else {
              this.selectedItems.set([]);
            }
          }
        });
    }
  }

  onFocus(): void {
    if (!this.disabled() && !this.locked()) {
      this.hasFocus.set(true);
      this.searchSubject.next(this.searchControl.value || '');
    }
  }

  onBlur(): void {
    setTimeout(() => {
      this.hasFocus.set(false);
      this.highlightedIndex.set(-1);
    }, 200);
  }

  onKeyDown(event: KeyboardEvent): void {
    if (!this.showDropdown()) return;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.highlightNext();
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.highlightPrevious();
        break;
      case 'Enter':
        event.preventDefault();
        this.selectHighlighted();
        break;
      case 'Escape':
        event.preventDefault();
        this.hasFocus.set(false);
        this.inputElem.nativeElement.blur();
        break;
    }
  }

  highlightNext(): void {
    const maxIndex = this.filteredOptions().length - 1;
    const current = this.highlightedIndex();
    this.highlightedIndex.set(current < maxIndex ? current + 1 : 0);
  }

  highlightPrevious(): void {
    const maxIndex = this.filteredOptions().length - 1;
    const current = this.highlightedIndex();
    this.highlightedIndex.set(current > 0 ? current - 1 : maxIndex);
  }

  selectHighlighted(): void {
    const highlighted = this.highlightedIndex();
    if (highlighted >= 0 && highlighted < this.filteredOptions().length) {
      this.selectOption(this.filteredOptions()[highlighted]);
    } else if (this.settings().addIfNonExisting && this.showAddItem()) {
      this.addNewItem();
    }
  }

  selectOption(option: T): void {
    const settings = this.settings();

    if (settings.multiple) {
      const current = this.selectedItems();
      if (!this.isSelected(option) || !settings.unique) {
        const updated = [...current, option];

        this.selectedItems.set(updated);
        this.emitSelection(updated);
      }
      this.searchControl.setValue('');
    } else {
      this.selectedItems.set([option]);
      this.emitSelection(option);

      this.hasFocus.set(false);
      this.searchControl.setValue(this.getDisplayText(option));
    }
  }

  removeItem(item: T): void {
    if (this.disabled() || this.locked()) return;

    const updated = this.selectedItems().filter(selected => !this.compareItems(selected, item));
    this.selectedItems.set(updated);

    if (this.settings().multiple) {
      this.emitSelection(updated);
    } else {
      this.emitSelection(null as any);
      this.searchControl.setValue('');
    }
  }

  addNewItem(): void {
    if (!this.settings().addIfNonExisting || !this.searchControl.value) return;

    const newItem = this.settings().addTransformFn(this.searchControl.value);

    if (this.settings().multiple) {
      const updated = [...this.selectedItems(), newItem];
      this.selectedItems.set(updated);
      this.newItemAdded.emit(updated);
      this.emitSelection(updated);
    } else {
      this.selectedItems.set([newItem]);
      this.newItemAdded.emit(newItem);
      this.emitSelection(newItem);
    }

    this.searchControl.setValue('');
    this.hasFocus.set(false);
  }

  clearSelection(): void {
    if (this.disabled() || this.locked()) return;

    this.selectedItems.set([]);
    this.searchControl.setValue('');
    this.emitSelection(this.settings().multiple ? [] : null as any);
  }

  toggleLocked(): void {
    const newLocked = !this.locked();
    this.lockedChange.emit(newLocked);
  }

  isSelected(option: T): boolean {
    return this.selectedItems().some(selected => this.compareItems(selected, option));
  }

  private compareItems(a: T, b: T): boolean {
    const settings = this.settings();
    if (settings.selectionCompareFn) {
      return settings.selectionCompareFn(a, b);
    }
    return a === b;
  }

  private emitSelection(value: T[] | T): void {
    this.selectedData.emit(value);

    // Update form control if provided
    const settings = this.settings();
    if (settings.formControl) {
      settings.formControl.setValue(value, { emitEvent: false });
    }
  }

  private getCurrentValue(): T[] | T | null {
    const items = this.selectedItems();
    if (this.settings().multiple) {
      return items;
    }
    return items.length > 0 ? items[0] : null;
  }

  getDisplayText(item: T): string {
    if (typeof item === 'string') return item;
    if (typeof item === 'object' && item !== null && 'name' in item) {
      return (item as any).name;
    }
    if (typeof item === 'object' && item !== null && 'value' in item) {
      return (item as any).value;
    }
    return String(item);
  }

  getPlaceholder(): string {
    if (this.locked()) return 'Field is locked';
    if (this.disabled()) return '';
    if (this.settings().multiple && this.hasSelections()) return 'Add more...';
    return 'Search...';
  }

}
