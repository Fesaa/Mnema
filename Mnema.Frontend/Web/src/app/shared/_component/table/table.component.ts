import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ContentChild, effect,
  EventEmitter,
  input,
  OnInit,
  Output,
  signal,
  TemplateRef
} from '@angular/core';
import {NgTemplateOutlet} from '@angular/common';
import {CdkDrag, CdkDragDrop, CdkDropList} from '@angular/cdk/drag-drop';


@Component({
  selector: 'app-table',
  imports: [
    NgTemplateOutlet,
    CdkDropList,
    CdkDrag
  ],
  templateUrl: './table.component.html',
  styleUrl: './table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TableComponent<T> implements OnInit {

  @ContentChild('header') headerTemplate!: TemplateRef<any>;
  @ContentChild('cell') cellTemplate!: TemplateRef<any>;
  @ContentChild('empty') emptyTemplate!: TemplateRef<any>;

  trackByIdFunc = input.required<(index: number, value: T) => string>();
  items = input.required<T[]>();
  pagination = input(true);
  pageSize = input(10);

  dragAble = input(false);
  dragTableId = input<string>();
  @Output() onDrop = new EventEmitter<CdkDragDrop<T[]>>();

  noHoverColour = input(false);

  currentPage = signal(1);

  totalPages = computed(() => Math.ceil(this.items().length / this.pageSize()));

  paginatedItems = computed(() => {
    if (!this.pagination()) return this.items();
    const start = (this.currentPage() - 1) * this.pageSize();
    const end = start + this.pageSize();
    return this.items().slice(start, end);
  });

  constructor() {
    // Reset current page if total page changes and becomes smaller
    effect(() => {
      if (this.currentPage() > this.totalPages()) {
        this.currentPage.set(1);
      }
    });
  }

  range = (n: number) => Array.from({ length: n}, (_, i) => i);

  ngOnInit(): void {
  }

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
