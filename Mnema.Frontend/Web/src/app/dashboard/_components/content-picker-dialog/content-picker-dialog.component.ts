import {ChangeDetectionStrategy, Component, computed, inject, model, OnInit, signal} from '@angular/core';
import {InfoStat} from '../../../_models/stats';
import {ContentService} from '../../../_services/content.service';
import {ListContentData} from '../../../_models/messages';
import {ToastService} from '../../../_services/toast.service';
import {TranslocoDirective} from '@jsverse/transloco';
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {LoadingSpinnerComponent} from "../../../shared/_component/loading-spinner/loading-spinner.component";
import {NgTemplateOutlet} from "@angular/common";
import {BadgeComponent} from "../../../shared/_component/badge/badge.component";

@Component({
  selector: 'app-content-picker-dialog',
  standalone: true,
  imports: [TranslocoDirective, LoadingSpinnerComponent, NgTemplateOutlet, BadgeComponent],
  templateUrl: './content-picker-dialog.component.html',
  styleUrls: ['./content-picker-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContentPickerDialogComponent implements OnInit {

  private readonly contentService = inject(ContentService);
  private readonly toastService = inject(ToastService);
  private readonly modal = inject(NgbActiveModal);

  info = model.required<InfoStat>();

  content = signal<ListContentData[]>([]);
  selection = signal<string[]>([]);
  loading = signal(true);

  toggles = signal<Set<string>>(new Set());
  allToggled = computed(() => this.content().length === this.toggles().size);

  ngOnInit(): void {
    this.loading.set(true);
    this.contentService.listContent(this.info().provider, this.info().id).subscribe({
      next: contents => {
        this.content.set(contents);
        this.selection.set(this.getAllSubContentIds(contents, true));
        this.loading.set(false);
      },
      error: err => {
        this.toastService.genericError(err?.error?.message ?? 'Unknown error');
        this.loading.set(false);
      }
    });
  }

  toggleUIAll() {
    const any = this.toggles().size > 0;
    const set = new Set<string>();

    if (!any) {
      this.content().forEach(item => set.add(item.label));
    }

    this.toggles.set(set);
  }

  toggleUI(id: string) {
    this.toggles.update(cur => {
      if (cur.has(id)) {
        cur.delete(id);
      } else {
        cur.add(id);
      }

      return cur;
    });
  }

  isSelected(lcd: ListContentData): boolean {
    if (lcd.subContentId) {
      return this.selection().includes(lcd.subContentId);
    }

    return lcd.children.filter(item => this.isSelected(item)).length === lcd.children.length;
  }

  unselectAll(): void {
    this.selection.set([]);
  }

  selectAll(): void {
    this.selection.set(this.getAllSubContentIds(this.content()));
  }

  /**
   * Currently assumes only one layer of children
   * @param lcd
   */
  toggle(lcd: ListContentData) {
    if (lcd.subContentId) {
      const id = lcd.subContentId;

      this.selection.update(x => {
        if (x.includes(id)) {
          return x.filter(item => item !== id);
        }

        return [...x, id];
      });
      return;
    }

    if (!lcd.children) return;

    const selected = this.isSelected(lcd);
    let selection = [...this.selection()];

    const toggleChildren = (children: ListContentData[]) => {
      for (const child of children) {
        if (child.subContentId) {
          if (selected) {
            selection = selection.filter(x => x !== child.subContentId);
          } else if (!selection.includes(child.subContentId)) {
            selection.push(child.subContentId);
          }
        }

        if (child.children) {
          toggleChildren(child.children);
        }
      }
    };

    toggleChildren(lcd.children);
    this.selection.set(selection);
  }

  reverse(): void {
    this.content.update(c => [...c].reverse());
  }

  close(): void {
    this.modal.close();
  }

  submit(): void {
    const ids = this.selection();

    if (ids.length === 0) {
      this.toastService.warningLoco('dashboard.content-picker.toasts.no-changes');
      return;
    }

    this.contentService.setFilter(this.info().provider, this.info().id, ids).subscribe({
      next: () => {
        this.toastService.successLoco(
          'dashboard.content-picker.toasts.success',
          {},
          { amount: ids.length, title: this.info().name }
        );
      },
      error: err => {
        this.toastService.genericError(err?.error?.message ?? 'Unknown error');
      },
    }).add(() => {
      this.close();
    });
  }

  private getAllSubContentIds(list: ListContentData[], requiredSelected: boolean = false): string[] {
    const result: string[] = [];

    function traverse(items: ListContentData[]): void {
      for (const item of items) {
        if (item.subContentId && !result.includes(item.subContentId)) {

          if (item.selected || !requiredSelected) {
            result.push(item.subContentId);
          }
        }

        if (item.children?.length) {
          traverse(item.children);
        }
      }
    }

    traverse(list);
    return result;
  }

  trackByContent(lcd: ListContentData): string {
    if (lcd.subContentId) return lcd.subContentId;

    return lcd.children.map(child => this.trackByContent(child)).join('-');
  }

  protected readonly parseInt = parseInt;
}
