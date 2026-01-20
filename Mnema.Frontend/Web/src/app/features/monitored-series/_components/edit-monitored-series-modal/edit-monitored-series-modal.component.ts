import {ChangeDetectionStrategy, Component, computed, inject, model, OnInit, signal} from '@angular/core';
import {ToastService} from "@mnema/_services/toast.service";
import {FormGroup, ReactiveFormsModule} from "@angular/forms";
import {TranslocoDirective} from "@jsverse/transloco";
import {GenericFormComponent} from "@mnema/generic-form/generic-form.component";
import {MonitoredSeries, MonitoredSeriesService} from "@mnema/features/monitored-series/monitored-series.service";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";
import {GenericFormFactoryService} from "@mnema/generic-form/generic-form-factory.service";
import {FormDefinition} from "@mnema/generic-form/form";
import {tap} from "rxjs";

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

  formDefinition = signal<FormDefinition | undefined>(undefined);

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
