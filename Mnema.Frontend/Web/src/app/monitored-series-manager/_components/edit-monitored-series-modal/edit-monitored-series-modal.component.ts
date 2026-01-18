import {ChangeDetectionStrategy, Component, computed, inject, model, OnInit, signal} from '@angular/core';
import {MonitoredSeries} from "../../../_services/monitored-series.service";
import {FormGroup, ReactiveFormsModule} from '@angular/forms';
import {ToastService} from "../../../_services/toast.service";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {TranslocoDirective} from "@jsverse/transloco";
import {MonitoredSeriesService} from "../../../_services/monitored-series.service";
import {FormControlDefinition, FormDefinition} from "../../../generic-form/form";
import {tap} from "rxjs";
import {GenericFormComponent} from "../../../generic-form/generic-form.component";
import {GenericFormFactoryService} from "../../../generic-form/generic-form-factory.service";

@Component({
  selector: 'app-edit-monitored-series-modal',
  imports: [
    ReactiveFormsModule,
    TranslocoDirective,
    GenericFormComponent
  ],
  templateUrl: './edit-monitored-series-modal.component.html',
  styleUrl: './edit-monitored-series-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditMonitoredSeriesModalComponent implements OnInit {

  private readonly toastService = inject(ToastService);
  private readonly monitoredSeriesService = inject(MonitoredSeriesService);
  private readonly modal = inject(NgbActiveModal);
  private readonly genericFormFactoryService = inject(GenericFormFactoryService);

  series = model.required<MonitoredSeries>();
  metadata = model.required<FormControlDefinition[]>();

  formDefinition = signal<FormDefinition | undefined>(undefined);
  optionsFormDefinition = computed(() => {
    const form = this.formDefinition();
    if (!form) return null;

    return {
      key: form.key,
      descriptionKey: '',
      controls: this.metadata().filter(d => !d.advanced),
    }
  });
  advancedFormDefinition = computed(() => {
    const form = this.formDefinition();
    if (!form) return null;

    return {
      key: form.key,
      descriptionKey: '',
      controls: this.metadata().filter(d => d.advanced),
    }
  });

  activeTab: 'general' | 'options' | 'advanced' = 'general';

  seriesForm = new FormGroup({});

  ngOnInit(): void {
    this.monitoredSeriesService.getForm().pipe(
      tap(form => this.formDefinition.set(form)),
    ).subscribe();
  }

  close() {
    this.modal.dismiss();
  }

  save() {
    const seriesValue = {
      ...this.series(),
      ...this.genericFormFactoryService.adjustForGenericMetadata(this.seriesForm.value),
    };

    const actions$ = this.series().id === ''
      ? this.monitoredSeriesService.new(seriesValue)
      : this.monitoredSeriesService.update(seriesValue);
    const kind = this.series().id === '' ? 'new' : 'update';

    actions$.subscribe({
      next: () => {
        this.toastService.successLoco(`monitored-series.toasts.${kind}.success`, {name: seriesValue.title});
      },
      error: err => {
        this.toastService.errorLoco(`monitored-series.toasts.${kind}.error`, {name: seriesValue.title}, {msg: err.error.message});
      }
    }).add(() => this.modal.close());
  }

}
