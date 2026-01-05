import {Component, inject, OnDestroy} from '@angular/core';
import {TranslocoDirective} from "@jsverse/transloco";
import {ButtonGridComponent} from "../button-grid/button-grid.component";
import {ButtonGroupService} from "../button-grid/button-group.service";
import {NavService} from "../_services/nav.service";

@Component({
  selector: 'app-dashboard',
  imports: [
    TranslocoDirective,
    ButtonGridComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnDestroy {

  private readonly navService = inject(NavService);

  protected readonly buttonGroupService = inject(ButtonGroupService);

  constructor() {
    this.navService.setNavVisibility(false);
  }

  ngOnDestroy(): void {
    this.navService.setNavVisibility(true);
  }

}
