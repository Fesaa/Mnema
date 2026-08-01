import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnDestroy,
  OnInit,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';

import { NavService } from '@mnema/_services/nav.service';
import { FormDefinition } from '@mnema/generic-form/form';
import { GenericFormComponent } from '@mnema/generic-form/generic-form.component';

@Component({
  selector: 'app-generic-form-playground',
  standalone: true,
  imports: [
    FormsModule,
    GenericFormComponent
  ],
  templateUrl: './generic-form-playground.component.html',
  styleUrl: './generic-form-playground.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GenericFormPlaygroundComponent implements OnInit, OnDestroy {

  private readonly navService = inject(NavService);

  protected readonly formDefinition = signal<FormDefinition | null>(null);
  protected readonly error = signal<string | null>(null);

  protected json = `{
  "key": "",
  "descriptionKey": "",
  "controls": []
  }`;

  ngOnInit() {
    this.navService.setNavVisibility(false);
  }

  ngOnDestroy() {
    this.navService.setNavVisibility(true);
  }

  protected loadDefinition(): void {
    this.error.set(null);

    try {
      const parsed = JSON.parse(this.json) as FormDefinition;
      this.formDefinition.set(parsed);
    } catch (e) {
      this.error.set(e instanceof Error ? e.message : 'Invalid JSON');
    }
  }
}
