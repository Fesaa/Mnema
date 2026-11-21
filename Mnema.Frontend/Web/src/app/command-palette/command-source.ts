import {InjectionToken} from '@angular/core';
import {Observable} from 'rxjs';
import {CommandGroup} from './command.model';

export interface CommandSource {
  key: string;
  minQueryLength: number;
  debounceMs: number;
  onlyWhenEmpty: boolean;
  search(query: string): Observable<CommandGroup[]>;
}

export const COMMAND_SOURCES = new InjectionToken<CommandSource[]>('COMMAND_SOURCES');
