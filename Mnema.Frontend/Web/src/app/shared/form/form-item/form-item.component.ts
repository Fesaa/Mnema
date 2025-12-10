import {Component, input, model} from '@angular/core';
import {FormControl} from "@angular/forms";

@Component({
  selector: 'app-form-item',
  imports: [],
  templateUrl: './form-item.component.html',
  styleUrl: './form-item.component.scss'
})
export abstract class FormItemComponent {

  id = model<string | undefined>(undefined);

  title = input.required<string>();
  subTitle = input<string | undefined>(undefined);
  toolTip = input<string | undefined>(undefined);

  control = input.required<FormControl>();

  constructor() {
    const id = this.id();
    if (!id) {
      this.id.set(this.generateId());
    }
  }

  private generateId(): string {
    if (crypto && crypto.randomUUID) {
      return crypto.randomUUID();
    }

    // non secure connections
    return 'id-' + Math.random().toString(36).substring(2, 9) + '-' + Date.now().toString(36);
  }

}
