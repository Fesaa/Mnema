import {ChangeDetectionStrategy, Component, inject, model} from '@angular/core';
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {AllModifierTypes, ModifierType} from "../../../../../../_models/page";
import {TranslocoDirective} from "@jsverse/transloco";
import {FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators} from "@angular/forms";
import {SettingsItemComponent} from "../../../../../../shared/form/settings-item/settings-item.component";
import {ModifierTypePipe} from "../../../../../../_pipes/modifier-type.pipe";
import {DefaultValuePipe} from "../../../../../../_pipes/default-value.pipe";

@Component({
  selector: 'app-edit-page-modifier-modal',
  imports: [
    TranslocoDirective,
    SettingsItemComponent,
    ReactiveFormsModule,
    ModifierTypePipe,
    DefaultValuePipe
  ],
  templateUrl: './edit-page-modifier-modal.component.html',
  styleUrl: './edit-page-modifier-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditPageModifierModalComponent {

  private readonly modal = inject(NgbActiveModal);

  modifierForm = model.required<FormGroup>();

  get valuesFormArray(): FormArray<FormGroup<{key: FormControl<string | null>, value: FormControl<string | null>, default: FormControl<boolean | null>}>> {
    return this.modifierForm().get('values') as unknown as FormArray;
  }

  addModifier() {
    this.valuesFormArray.push(new FormGroup({
      key: new FormControl('', [Validators.required]),
      value: new FormControl('', [Validators.required]),
      default: new FormControl(false, []),
    }));
  }

  deleteModifier(idx: number) {
    this.valuesFormArray.removeAt(idx);
  }

  close() {
    this.modal.close();
  }

  save() {
    if (!this.modifierForm().valid) return;
    this.close()
  }

  protected readonly AllModifierTypes = AllModifierTypes;
  protected readonly ModifierType = ModifierType;
}
