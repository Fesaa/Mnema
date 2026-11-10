import {
  ChangeDetectionStrategy,
  Component,
  ContentChild,
  computed,
  EventEmitter,
  input,
  output,
  TemplateRef
} from '@angular/core';
import {NgTemplateOutlet} from '@angular/common';
import {CdkDrag, CdkDragDrop, CdkDropList} from '@angular/cdk/drag-drop';
import {of} from 'rxjs';
import {PageLoader, PaginatorComponent} from '../paginator/paginator.component';

@Component({
  selector: 'app-item-list',
  imports: [
    NgTemplateOutlet,
    CdkDropList,
    CdkDrag,
    PaginatorComponent,
  ],
  templateUrl: './item-list.component.html',
  styleUrl: './item-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ItemListComponent<T> {

  @ContentChild('row') rowTemplate!: TemplateRef<any>;
  @ContentChild('empty') emptyTemplate!: TemplateRef<any>;

  trackByIdFunc = input.required<(index: number, value: T) => string>();
  pageLoader = input<PageLoader<T>>();
  items = input<Array<T>>();
  reloader = input<EventEmitter<void>>(new EventEmitter());
  pageSize = input(20);
  dragAble = input(false);

  onDrop = output<CdkDragDrop<T[]>>();
  currentItems = output<T[]>();

  finalPageLoader = computed<PageLoader<T>>(() => {
    const pageLoader = this.pageLoader();
    const items = this.items();

    if (pageLoader && items) {
      throw new Error('Only one of PageLoader and Items may be set');
    }

    if (pageLoader) return pageLoader;

    if (items) {
      return () => of({
        items,
        totalPages: 1,
        currentPage: 0,
        pageSize: items.length,
        totalCount: items.length,
      });
    }

    throw new Error('PageLoader or Items must be set');
  });
}
