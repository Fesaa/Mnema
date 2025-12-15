import {
  Component,
  computed,
  ContentChild, effect, EventEmitter,
  inject,
  input, linkedSignal,
  OnInit, Output,
  signal,
  TemplateRef
} from '@angular/core';
import {NgTemplateOutlet} from "@angular/common";
import {isNumber, TranslocoDirective} from "@jsverse/transloco";
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
export class PaginatorComponent<T> implements OnInit {

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

  visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const maxVisible = 10;

    if (total <= maxVisible) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    const pages: (number | string)[] = [];

    pages.push(1);

    let start = Math.max(2, current - 1);
    let end = Math.min(total - 1, current + 1);

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

  constructor() {
    effect(() => {
      this.currentItems.emit(this.items());
    });
  }

  ngOnInit() {
    this.loadPage(this.startPage());
  }

  private loadPage(pageNumber: number) {
    this.pageLoader()(pageNumber, this.pageSize()).subscribe(pagedList => {
      const noResultKey = this.noResultsKey();
      const successKey = this.successKey();

      if (pagedList.totalCount === 0 && noResultKey) {
        this.toastService.errorLoco(noResultKey);
      } else if (pagedList.totalCount > 0 && successKey && pagedList.currentPage === 0) {
        this.toastService.successLoco(successKey, {}, { amount: pagedList.totalCount});
      }

      this.pagedList.set(pagedList)
    });
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.loadPage(page);
    }
  }

  nextPage(): void {
    this.goToPage(this.currentPage() + 1);
  }

  prevPage(): void {
    this.goToPage(this.currentPage() - 1);
  }


  protected readonly isNumber = isNumber;
}
