/**
 * This component has been adjusted from https://github.com/Kareadita/Kavita
 */
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ContentChild,
  ElementRef,
  HostListener,
  inject,
  input,
  model,
  OnChanges,
  SimpleChange,
  SimpleChanges,
  TemplateRef
} from '@angular/core';
import {AbstractControl} from '@angular/forms';
import {TranslocoDirective} from '@jsverse/transloco';
import {NgClass, NgTemplateOutlet} from '@angular/common';
import {SafeHtmlPipe} from '../../../_pipes/safe-html-pipe';
import {filter, fromEvent, tap} from 'rxjs';

@Component({
  selector: 'app-settings-item',
  imports: [
    TranslocoDirective,
    NgTemplateOutlet,
    NgClass,
    SafeHtmlPipe,
  ],
  templateUrl: './settings-item.component.html',
  styleUrl: './settings-item.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsItemComponent implements OnChanges {

  private readonly cdRef = inject(ChangeDetectorRef);
  private readonly elementRef = inject(ElementRef);

  control = input.required<AbstractControl>();

  title = input<string>();
  tooltip = input<string>();
  labelId = input<string>();

  editLabel = input<string>();
  canEdit = input(true);
  showEdit = input(true);
  isEditMode = model(false);

  toggleOnViewClick = input(true);

  /**
   * View in View mode
   */
  @ContentChild('view') valueViewRef!: TemplateRef<any>;
  /**
   * View in Edit mode
   */
  @ContentChild('edit') valueEditRef!: TemplateRef<any>;

  @HostListener('click', ['$event'])
  onClickInside(event: MouseEvent) {
    event.stopPropagation(); // Prevent the click from bubbling up
  }

  constructor(elementRef: ElementRef) {
    if (!this.toggleOnViewClick() || !this.showEdit()) return;

    fromEvent(window, 'click')
      .pipe(
        filter((event: Event) => {
          if (!this.toggleOnViewClick()) return false;
          if (!this.showEdit()) return false;
          if (this.isEditMode() && this.control().dirty && this.control().invalid) return false;

          const mouseEvent = event as MouseEvent;
          const selection = window.getSelection();
          const hasSelection = selection !== null && selection.toString().trim() === '';
          return !elementRef.nativeElement.contains(mouseEvent.target) && hasSelection;
        }),
        tap(() => {
          this.isEditMode.set(false)
          this.cdRef.markForCheck();
        })
      )
      .subscribe();
  }

  toggleEditMode() {
    if (!this.toggleOnViewClick()) return;
    if (!this.canEdit()) return;
    if (this.isEditMode() && this.control().dirty && this.control().invalid) return;

    this.isEditMode.update(b => !b);
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes.hasOwnProperty('isEditMode')) {
      const change = changes['isEditMode'] as SimpleChange;
      if (change.isFirstChange()) return;

      if (!this.toggleOnViewClick()) return;
      if (!this.canEdit()) return;
      if (this.isEditMode() && this.control().dirty && this.control().invalid) return;

      this.isEditMode = change.currentValue;
      this.cdRef.markForCheck();

      this.focusInput();
    }
  }

  focusInput() {
    if (!this.isEditMode()) return;

    setTimeout(() => {
      const inputElem = this.findFirstInput();
      if (inputElem) {
        inputElem.focus();
      }
    }, 10);
  }

  private findFirstInput(): HTMLInputElement | null {
    const nativeInputs = [...this.elementRef.nativeElement.querySelectorAll('input'), ...this.elementRef.nativeElement.querySelectorAll('select'), ...this.elementRef.nativeElement.querySelectorAll('textarea')];
    if (nativeInputs.length === 0) return null;

    return nativeInputs[0];
  }

}
