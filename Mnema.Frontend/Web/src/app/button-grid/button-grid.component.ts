import {Component, computed, inject, input, TemplateRef, viewChild} from '@angular/core';
import {CommonModule} from '@angular/common';
import {Breakpoint, UtilityService} from "../_services/utility.service";
import {NavigationBehaviorOptions, NavigationExtras, Router, UrlTree} from "@angular/router";
import {Button, ButtonGroup, ButtonGroupService} from "./button-group.service";
import {ModalService} from "../_services/modal.service";
import {
  ListSelectionItem,
  ListSelectModalComponent
} from "../shared/_component/list-select-modal/list-select-modal.component";
import {tap} from "rxjs";
import {UpdateBadgeDirective} from "../_directives/update-badge.directive";
import {BadgeComponent} from "../shared/_component/badge/badge.component";

@Component({
  selector: 'app-button-grid',
  standalone: true,
  imports: [CommonModule, UpdateBadgeDirective, BadgeComponent],
  templateUrl: './button-grid.component.html',
  styleUrls: ['./button-grid.component.scss']
})
export class ButtonGridComponent {

  protected readonly utilityService = inject(UtilityService);
  protected readonly buttonGroupService = inject(ButtonGroupService);

  groups = input.required<ButtonGroup[]>();
  mobileMode = computed(() => this.utilityService.breakPoint() < Breakpoint.Desktop );

  listSelectTemplate = viewChild.required<TemplateRef<ListSelectionItem<Button>>>('listSelectOption');
}
