import {ChangeDetectionStrategy, Component, input} from '@angular/core';
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {TranslocoDirective} from "@jsverse/transloco";

@Component({
  selector: 'app-wiki-link',
  imports: [
    NgbTooltip,
    TranslocoDirective
  ],
  templateUrl: './wiki-link.component.html',
  styleUrl: './wiki-link.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WikiLinkComponent {

  wikiLink = input.required<string | undefined>();

}
