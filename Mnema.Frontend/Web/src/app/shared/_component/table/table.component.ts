import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ContentChild,
  EventEmitter,
  inject,
  input,
  Output,
  TemplateRef
} from '@angular/core';
import {NgTemplateOutlet} from '@angular/common';
import {CdkDrag, CdkDragDrop, CdkDropList} from '@angular/cdk/drag-drop';
import {PageLoader, PaginatorComponent} from "../paginator/paginator.component";
import {of} from "rxjs";
import {Breakpoint, UtilityService} from "../../../_services/utility.service";


@Component({
  selector: 'app-table',
  imports: [
    NgTemplateOutlet,
    CdkDropList,
    CdkDrag,
    PaginatorComponent
  ],
  templateUrl: './table.component.html',
  styleUrl: './table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TableComponent<T> {

  protected readonly utilityService = inject(UtilityService);

  @ContentChild('header') headerTemplate!: TemplateRef<any>;
  @ContentChild('cell') cellTemplate!: TemplateRef<any>;
  @ContentChild('empty') emptyTemplate!: TemplateRef<any>;
  @ContentChild('mobile') mobileTemplate!: TemplateRef<any>;

  trackByIdFunc = input.required<(index: number, value: T) => string>();
  pageLoader = input<PageLoader<T>>();
  items = input<Array<T>>();
  reloader = input<EventEmitter<void>>(new EventEmitter());
  pageSize = input(20);

  dragAble = input(false);
  dragTableId = input<string>();
  @Output() onDrop = new EventEmitter<CdkDragDrop<T[]>>();

  noHoverColour = input(false);
  noMobileCards = input(false);

  finalPageLoader = computed<PageLoader<T>>(() => {
    const pageLoader = this.pageLoader();
    const items = this.items();

    if (pageLoader && items) {
      throw new Error("Only one of PageLoader and Items may be set");
    }

    if (pageLoader) return pageLoader;

    if (items) {
      return (pn, ps) => of({
        items: items,
        totalPages: 1,
        currentPage: 0,
        pageSize: items.length,
        totalCount: items.length,
      });
    }

    throw new Error("PageLoader or Items must be set");
  });

  @Output() currentItems = new EventEmitter<T[]>();
  protected readonly Breakpoint = Breakpoint;
}
