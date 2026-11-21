import {inject, Injectable} from '@angular/core';
import {Router} from '@angular/router';
import {translate} from '@jsverse/transloco';
import {Observable, of} from 'rxjs';
import {MetadataProvider} from '@mnema/features/monitored-series/metadata.service';
import {CommandGroup, CommandItem} from '../command.model';
import {CommandSource} from '../command-source';

const GROUP_KEY = 'metadata-search';
const GROUP_ORDER = 900;

const PROVIDER_SEARCHES = [
  {provider: MetadataProvider.Hardcover, labelKey: 'command-palette.search-metadata.hardcover'},
  {provider: MetadataProvider.Mangabaka, labelKey: 'command-palette.search-metadata.mangabaka'},
];

@Injectable()
export class MetadataSearchCommandSource implements CommandSource {

  readonly key = GROUP_KEY;
  readonly minQueryLength = 1;
  readonly debounceMs = 0;
  readonly onlyWhenEmpty = true;

  private readonly router = inject(Router);

  search(query: string): Observable<CommandGroup[]> {
    const term = query.trim();
    if (term.length < this.minQueryLength) return of([]);

    const items: CommandItem[] = PROVIDER_SEARCHES.map(search => ({
      id: `${GROUP_KEY}:${search.provider}`,
      title: translate(search.labelKey),
      icon: 'fa fa-magnifying-glass',
      run: () => {
        this.router.navigate(['/series-search'], {
          queryParams: {query: term, provider: search.provider},
        }).catch(err => console.error(err));
      },
    }));

    return of([{
      key: GROUP_KEY,
      title: translate('button-groups.actions.search-series'),
      order: GROUP_ORDER,
      items,
    }]);
  }
}
