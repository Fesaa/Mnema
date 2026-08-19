import {ChangeDetectionStrategy, Component, computed, inject, model} from '@angular/core';
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {Series} from "./_types";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {UtcToLocalTimePipe} from "../../../_pipes/utc-to-local.pipe";
import {PersonRolePipe} from "@mnema/_pipes/person-role.pipe";

@Component({
  selector: 'app-series-info',
  imports: [
    TranslocoDirective,
    UtcToLocalTimePipe,
    PersonRolePipe
  ],
  templateUrl: './series-info.component.html',
  styleUrl: './series-info.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeriesInfoComponent {

  private readonly modal = inject(NgbActiveModal);
  series = model.required<Series |null>();

  protected seriesTitle = computed(() => this.series()?.title ?? translate('shared.unknown'));
  protected coverUrl = computed(() => {
    const url = this.series()?.coverUrl;
    if (!url) return null;

    if (url.startsWith('proxy')) {
      return '/api/' + url;
    }

    return url;
  });

  close() {
    this.modal.dismiss();
  }



}
