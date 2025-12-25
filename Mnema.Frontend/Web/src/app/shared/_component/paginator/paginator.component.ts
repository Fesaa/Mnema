import {
  Component,
  computed,
  ContentChild,
  effect,
  EventEmitter,
  inject,
  input,
  linkedSignal,
  Output,
  signal,
  TemplateRef
} from '@angular/core';
import {NgTemplateOutlet} from "@angular/common";
import {TranslocoDirective} from "@jsverse/transloco";
import {EMPTY_PAGE, PagedList} from "../../../_models/paged-list";
import {Observable} from "rxjs";
import {ToastService} from "../../../_services/toast.service";

export type PageLoader<T> = (pageNumber: number, pageSize: number) => Observable<PagedList<T>>;

@Component({
  selector: 'app-paginator',
  imports: [
    NgTemplateOutlet,
    TranslocoDirective,
  ],
  templateUrl: './paginator.component.html',
  styleUrl: './paginator.component.scss'
})
export class PaginatorComponent<T> {

  private readonly toastService = inject(ToastService);

  @ContentChild("items") itemsTemplate!: TemplateRef<any>;

  pageLoader = input.required<PageLoader<T>>();
  pageSize = input(20);
  startPage = input(0);

  noResultsKey = input<string | null>('common.no-results');
  successKey = input<string | null>(null);

  pagedList = signal<PagedList<T>>(EMPTY_PAGE);
  totalPages = computed(() => this.pagedList().totalPages);

  currentPage = computed(() => this.pagedList().currentPage);
  currentPageDisplay = computed(() => this.currentPage() + 1);

  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.currentPageDisplay(); // Use 1-based for display calculations
    const maxVisible = 10;

    if (total <= maxVisible) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    const pages: (number | string)[] = [];
    pages.push(1);

    const sidePages = Math.floor((maxVisible - 2) / 2);

    let start = Math.max(2, current - sidePages);
    let end = Math.min(total - 1, current + sidePages);

    if (current <= sidePages + 2) {
      end = Math.min(total - 1, maxVisible - 1);
      start = 2;
    }

    if (current >= total - sidePages - 1) {
      start = Math.max(2, total - maxVisible + 2);
      end = total - 1;
    }

    if (start > 2) {
      pages.push('...');
    }

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    if (end < total - 1) {
      pages.push('...');
    }

    if (total > 1) {
      pages.push(total);
    }

    return pages;
  });

  items = linkedSignal(() => this.pagedList().items);

  @Output() currentItems = new EventEmitter<T[]>();
  @Output() noResults = new EventEmitter<void>();

  constructor() {
    effect(() => {
      this.currentItems.emit(this.items());
    });

    // Reload from start when page loader changes
    effect(() => {
      this.pageLoader();
      this.loadPage(this.startPage());
    });
  }

  private loadPage(pageNumber: number) {
    this.pageLoader()(pageNumber, this.pageSize()).subscribe(pagedList => {
      const noResultKey = this.noResultsKey();
      const successKey = this.successKey();

      if (pagedList.totalCount === 0 && noResultKey) {
        this.toastService.errorLoco(noResultKey);
        this.noResults.emit();
      } else if (pagedList.totalCount > 0 && successKey && pagedList.currentPage === 0) {
        this.toastService.successLoco(successKey, {}, { amount: pagedList.totalCount});
      }

      this.pagedList.set(pagedList);
    });
  }

  // Accept 1-based page number from UI, convert to 0-based for backend
  goToPage(pageDisplay: number | string): void {
    if (typeof pageDisplay === 'string') return;

    const pageZeroBased = pageDisplay - 1;

    if (pageZeroBased >= 0 && pageZeroBased < this.totalPages()) {
      this.loadPage(pageZeroBased);
    }
  }

  nextPage(): void {
    const nextPage = this.currentPage() + 1;
    if (nextPage < this.totalPages()) {
      this.loadPage(nextPage);
    }
  }

  prevPage(): void {
    const prevPage = this.currentPage() - 1;
    if (prevPage >= 0) {
      this.loadPage(prevPage);
    }
  }
}
