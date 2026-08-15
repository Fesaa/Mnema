import {ChangeDetectionStrategy, Component, inject, model, viewChild} from '@angular/core';
import {TranslocoDirective} from "@jsverse/transloco";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {SearchFormComponent} from "@mnema/page/_components/search-form/search-form.component";
import {Page} from "@mnema/_models/page";

@Component({
  selector: 'app-edit-page-defaults-modal',
  imports: [
    TranslocoDirective,
    SearchFormComponent
  ],
  templateUrl: './edit-page-defaults-modal.component.html',
  styleUrl: './edit-page-defaults-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditPageDefaultsModalComponent {

  private readonly modal = inject(NgbActiveModal);

  page = model.required<Page>();
  pageSearchForm = viewChild.required(SearchFormComponent);

  protected close() {
    this.modal.dismiss();
  }

  protected save() {
    this.modal.close(this.pageSearchForm().packModifiers());
  }
}
