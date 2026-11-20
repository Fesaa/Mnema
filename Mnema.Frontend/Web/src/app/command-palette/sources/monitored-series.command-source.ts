import {inject, Injectable} from '@angular/core';
import {Router} from '@angular/router';
import {translate} from '@jsverse/transloco';
import {catchError, map, Observable, of} from 'rxjs';
import {
  MonitoredSeriesLookup,
  MonitoredSeriesService
} from '@mnema/features/monitored-series/monitored-series.service';
import {CommandGroup} from '../command.model';
import {CommandSource} from '../command-source';

const GROUP_KEY = 'monitored-series';
const GROUP_ORDER = 50;
const RESULT_LIMIT = 5;

@Injectable()
export class MonitoredSeriesCommandSource implements CommandSource {

  readonly key = GROUP_KEY;
  readonly minQueryLength = 2;
  readonly debounceMs = 200;

  private readonly monitoredSeriesService = inject(MonitoredSeriesService);
  private readonly router = inject(Router);

  search(query: string): Observable<CommandGroup[]> {
    const term = query.trim();
    if (term.length < this.minQueryLength) return of([]);

    return this.monitoredSeriesService.lookup(term, RESULT_LIMIT).pipe(
      map(series => series.length === 0 ? [] : [this.toGroup(series)]),
      catchError(() => of([])),
    );
  }

  private toGroup(series: MonitoredSeriesLookup[]): CommandGroup {
    return {
      key: GROUP_KEY,
      title: translate('button-groups.actions.monitored-series'),
      order: GROUP_ORDER,
      items: series.map(s => ({
        id: `${GROUP_KEY}:${s.id}`,
        title: s.title,
        icon: 'fa fa-television',
        provider: s.provider,
        coverUrl: s.coverUrl,
        run: () => {
          this.router.navigate(['/monitored-series-detail', s.id]).catch(err => console.error(err));
        },
      })),
    };
  }
}
