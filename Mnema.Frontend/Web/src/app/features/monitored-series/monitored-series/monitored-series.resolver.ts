import {ResolveFn} from '@angular/router';
import {inject} from "@angular/core";
import {MonitoredSeries, MonitoredSeriesService} from "@mnema/features/monitored-series/monitored-series.service";

export const monitoredSeriesResolver: ResolveFn<MonitoredSeries> = (route, state) => {
  const service = inject(MonitoredSeriesService);

  return service.get(route.paramMap.get('id')!);
};
