import {ChangeDetectionStrategy, Component, ContentChild, input, TemplateRef} from '@angular/core';
import {AbstractControl, ReactiveFormsModule} from "@angular/forms";
import {SettingsItemComponent} from "../settings-item/settings-item.component";
import {NgTemplateOutlet} from "@angular/common";

@Component({
  selector: 'app-settings-switch',
  imports: [
    SettingsItemComponent,
    ReactiveFormsModule,
    NgTemplateOutlet
  ],
  templateUrl: './settings-switch.component.html',
  styleUrl: './settings-switch.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsSwitchComponent {

  @ContentChild('switch') valueSwitchRef!: TemplateRef<any>;

  control = input.required<AbstractControl>();

  title = input<string>();
  tooltip = input<string>();
  labelId = input<string>();

  canEdit = input(true);

  toggleOnViewClick = input(true);
}
