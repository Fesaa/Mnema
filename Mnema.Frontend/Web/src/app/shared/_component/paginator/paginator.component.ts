import {Component, computed, ContentChild, effect, input, signal, TemplateRef} from '@angular/core';
import {NgTemplateOutlet} from "@angular/common";
import {TranslocoDirective} from "@jsverse/transloco";

@Component({
  selector: 'app-paginator',
  imports: [
    NgTemplateOutlet,
    TranslocoDirective
  ],
  templateUrl: './paginator.component.html',
  styleUrl: './paginator.component.scss'
})
export class PaginatorComponent<T> {

  @ContentChild("items") itemsTemplate!: TemplateRef<any>;

  items = input.required<T[]>();
  pageSize = input(10);

  currentPage = signal(1);
  totalPages = computed(() => Math.ceil(this.items().length / this.pageSize()));
  visibleItems = computed(() => {
    const page = this.currentPage();
    const pageSize = this.pageSize();
    const items = this.items();

    return items.slice((page-1) * pageSize, page * pageSize);
  })

  constructor() {
    effect(() => {
      this.items();
      this.currentPage.set(1);
    });
  }

  range = (n: number) => Array.from({ length: n}, (_, i) => i);

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  nextPage(): void {
    this.goToPage(this.currentPage() + 1);
  }

  prevPage(): void {
    this.goToPage(this.currentPage() - 1);
  }


}
