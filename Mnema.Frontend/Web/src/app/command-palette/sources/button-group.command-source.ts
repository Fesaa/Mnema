import {inject, Injectable} from '@angular/core';
import {translate} from '@jsverse/transloco';
import {Observable, of} from 'rxjs';
import {
  Button,
  ButtonGroup,
  ButtonGroupKey,
  ButtonGroupService
} from '@mnema/button-grid/button-group.service';
import {CommandGroup, CommandItem} from '../command.model';
import {CommandSource} from '../command-source';
import {commandQueryTokens, scoreCommandText} from '../command-score';

const NAVIGATION_GROUP_KEY = 'navigation';
const MAX_RESULTS_PER_GROUP = 8;

const GROUP_KEYS: Record<ButtonGroupKey, string> = {
  [ButtonGroupKey.Actions]: 'actions',
  [ButtonGroupKey.Pages]: 'pages',
  [ButtonGroupKey.Settings]: 'settings',
  [ButtonGroupKey.Any]: 'any',
};

@Injectable()
export class ButtonGroupCommandSource implements CommandSource {

  readonly key = 'button-groups';
  readonly minQueryLength = 1;
  readonly debounceMs = 0;

  private readonly buttonGroupService = inject(ButtonGroupService);

  search(query: string): Observable<CommandGroup[]> {
    const tokens = commandQueryTokens(query);

    const groups: CommandGroup[] = [
      this.navigationGroup(tokens),
      ...this.buttonGroupService.dashboardGroups().map((group, index) =>
        this.toCommandGroup(group, 20 + index * 10, tokens)),
    ];

    return of(groups.filter(group => group.items.length > 0));
  }

  private navigationGroup(tokens: string[]): CommandGroup {
    return this.toGroup(NAVIGATION_GROUP_KEY, translate('nav-bar.navigation'), 10, tokens, [
      {
        title: translate('nav-bar.home'),
        icon: 'fa fa-home',
        navUrl: '/home',
      },
      {
        title: translate('button-groups.settings.title'),
        icon: 'fa fa-cogs',
        navUrl: '/settings/preferences',
      },
    ]);
  }

  private toCommandGroup(group: ButtonGroup, order: number, tokens: string[]): CommandGroup {
    const key = `button-group:${GROUP_KEYS[group.key]}`;

    return this.toGroup(key, group.title, order, tokens, group.buttons);
  }

  private toGroup(key: string, title: string, order: number, tokens: string[], buttons: Button[]): CommandGroup {
    const scored = buttons.map((button, index) => ({
      button,
      index,
      score: tokens.length === 0 ? 1 : scoreCommandText(tokens, button.title, title, ...(button.tags ?? [])),
    })).filter(entry => entry.score > 0);

    if (tokens.length > 0) {
      scored.sort((a, b) => b.score - a.score || a.index - b.index);
    }

    const visible = tokens.length === 0 ? scored : scored.slice(0, MAX_RESULTS_PER_GROUP);

    return {
      key,
      title,
      order,
      items: visible.map(({button, index}) => this.toItem(key, title, index, button)),
    };
  }

  private toItem(groupKey: string, groupTitle: string, index: number, button: Button): CommandItem {
    return {
      id: `${groupKey}:${index}`,
      title: button.title,
      subtitle: groupTitle,
      icon: button.icon,
      run: () => this.buttonGroupService.handleButtonClick(button),
    };
  }
}
