import {
  afterNextRender,
  afterRenderEffect,
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  Signal,
  signal,
  viewChild,
  viewChildren,
} from '@angular/core';
import {toObservable, toSignal} from '@angular/core/rxjs-interop';
import {NgbActiveModal} from '@ng-bootstrap/ng-bootstrap';
import {TranslocoDirective} from '@jsverse/transloco';
import {map, of, startWith, switchMap, timer} from 'rxjs';
import {ProviderNamePipe} from '@mnema/_pipes/provider-name.pipe';
import {LoadingSpinnerComponent} from '@mnema/shared/_component/loading-spinner/loading-spinner.component';
import {CommandGroup, CommandItem} from './command.model';
import {COMMAND_SOURCES, CommandSource} from './command-source';

interface SourceState {
  loading: boolean;
  groups: CommandGroup[];
}

const EMPTY_SOURCE_STATE: SourceState = {
  loading: false,
  groups: [],
};

@Component({
  selector: 'app-command-palette',
  imports: [TranslocoDirective, ProviderNamePipe, LoadingSpinnerComponent],
  templateUrl: './command-palette.component.html',
  styleUrl: './command-palette.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommandPaletteComponent {

  private readonly sources = inject(COMMAND_SOURCES);
  private readonly activeModal = inject(NgbActiveModal);

  protected query = signal('');
  protected activeIndex = signal(0);

  private readonly query$ = toObservable(this.query);

  private readonly primaryStates = this.statesFor(false);
  private readonly fallbackStates = this.statesFor(true);

  protected readonly hasQuery = computed(() => this.query().trim().length > 0);

  private readonly primaryGroups = computed<CommandGroup[]>(() => this.primaryStates
    .flatMap(state => state().groups)
    .sort((a, b) => a.order - b.order));

  private readonly fallbackGroups = computed<CommandGroup[]>(() => this.fallbackStates
    .flatMap(state => state().groups)
    .sort((a, b) => a.order - b.order));

  protected readonly isLoading = computed(() =>
    this.primaryStates.some(state => state().loading)
    || (this.primaryGroups().length === 0 && this.fallbackStates.some(state => state().loading)));

  protected readonly hasRealResults = computed(() => this.primaryGroups().length > 0);

  protected readonly displayGroups = computed<CommandGroup[]>(() => {
    if (this.hasRealResults() || this.isLoading()) return this.primaryGroups();
    return this.fallbackGroups();
  });

  protected readonly displayItems = computed<CommandItem[]>(() => this.displayGroups()
    .flatMap(group => group.items));

  protected readonly selectedIndex = computed(() => {
    const count = this.displayItems().length;
    return count === 0 ? 0 : Math.min(this.activeIndex(), count - 1);
  });

  protected readonly activeItem = computed(() => this.displayItems()[this.selectedIndex()]);

  protected readonly activeDescendantId = computed(() => {
    const item = this.activeItem();
    return item ? `command-palette-item-${item.id}` : null;
  });

  private readonly queryInput = viewChild<ElementRef<HTMLInputElement>>('queryInput');
  private readonly itemElements = viewChildren<ElementRef<HTMLElement>>('itemElement');

  constructor() {
    afterNextRender(() => this.queryInput()?.nativeElement.focus());

    afterRenderEffect(() => {
      const element = this.itemElements()[this.selectedIndex()]?.nativeElement;
      element?.scrollIntoView({block: 'nearest'});
    });
  }

  protected onQueryInput(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
    this.activeIndex.set(0);
  }

  protected onKeydown(event: KeyboardEvent): void {
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.move(1);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.move(-1);
        break;
      case 'Enter':
        event.preventDefault();
        this.run(this.activeItem());
        break;
    }
  }

  protected isActive(item: CommandItem): boolean {
    return this.activeItem()?.id === item.id;
  }

  protected selectItem(item: CommandItem): void {
    const index = this.displayItems().findIndex(candidate => candidate.id === item.id);
    if (index >= 0) {
      this.activeIndex.set(index);
    }
  }

  protected run(item: CommandItem | undefined): void {
    if (!item) return;

    this.close();
    item.run();
  }

  protected close(): void {
    this.activeModal.dismiss();
  }

  private statesFor(onlyWhenEmpty: boolean): Signal<SourceState>[] {
    return this.sources
      .filter(source => source.onlyWhenEmpty === onlyWhenEmpty)
      .map(source => this.createSourceState(source));
  }

  private createSourceState(source: CommandSource): Signal<SourceState> {
    const state = toSignal<SourceState>(
      this.query$.pipe(
        switchMap(query => {
          const term = query.trim();
          if (term.length < source.minQueryLength) return of(EMPTY_SOURCE_STATE);

          const search$ = source.debounceMs > 0
            ? timer(source.debounceMs).pipe(switchMap(() => source.search(term)))
            : source.search(term);

          return search$.pipe(
            map(groups => ({loading: false, groups})),
            startWith({loading: true, groups: [] as CommandGroup[]}),
          );
        }),
      ),
    );

    return computed<SourceState>(() => state() ?? EMPTY_SOURCE_STATE);
  }

  private move(delta: number): void {
    const count = this.displayItems().length;
    if (count === 0) return;

    this.activeIndex.set((this.selectedIndex() + delta + count) % count);
  }
}
