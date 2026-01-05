import {Component, computed, inject, input, TemplateRef, viewChild} from '@angular/core';
import {CommonModule} from '@angular/common';
import {Breakpoint, UtilityService} from "../_services/utility.service";
import {Button, ButtonGroup, ButtonGroupService} from "./button-group.service";
import {
  ListSelectionItem
} from "../shared/_component/list-select-modal/list-select-modal.component";
import {BadgeComponent} from "../shared/_component/badge/badge.component";

@Component({
  selector: 'app-button-grid',
  standalone: true,
  imports: [CommonModule, BadgeComponent],
  templateUrl: './button-grid.component.html',
  styleUrls: ['./button-grid.component.scss']
})
export class ButtonGridComponent {

  protected readonly utilityService = inject(UtilityService);
  protected readonly buttonGroupService = inject(ButtonGroupService);

  groups = input.required<ButtonGroup[]>();
  mobileColumns = input<number>(1);
  mobileMode = computed(() => this.utilityService.breakPoint() < Breakpoint.Desktop );

  listSelectTemplate = viewChild.required<TemplateRef<ListSelectionItem<Button>>>('listSelectOption');
}
