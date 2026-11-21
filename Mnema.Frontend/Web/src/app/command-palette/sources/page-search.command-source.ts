import {inject, Injectable} from '@angular/core';
import {Router} from '@angular/router';
import {translate} from '@jsverse/transloco';
import {Observable, of} from 'rxjs';
import {PageService} from '@mnema/_services/page.service';
import {CommandGroup, CommandItem} from '../command.model';
import {CommandSource} from '../command-source';

const GROUP_KEY = 'page-search';
const GROUP_ORDER = 1000;

@Injectable()
export class PageSearchCommandSource implements CommandSource {

  readonly key = GROUP_KEY;
  readonly minQueryLength = 1;
  readonly debounceMs = 0;
  readonly onlyWhenEmpty = true;

  private readonly pageService = inject(PageService);
  private readonly router = inject(Router);

  search(query: string): Observable<CommandGroup[]> {
    const term = query.trim();
    if (term.length < this.minQueryLength) return of([]);

    const items: CommandItem[] = this.pageService.pages().map(page => ({
      id: `${GROUP_KEY}:${page.id}`,
      title: page.title,
      icon: `fa ${page.icon}`,
      run: () => {
        this.router.navigate(['/page'], {queryParams: {id: page.id, query: term}})
          .catch(err => console.error(err));
      },
    }));

    if (items.length === 0) return of([]);

    return of([{
      key: GROUP_KEY,
      title: translate('command-palette.search-with-label'),
      order: GROUP_ORDER,
      items,
    }]);
  }
}
