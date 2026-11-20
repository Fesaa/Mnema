import {Provider} from '@mnema/_models/page';

export interface CommandItem {
  id: string;
  title: string;
  subtitle?: string;
  icon: string;
  provider?: Provider;
  coverUrl?: string;
  run: () => void;
}

export interface CommandGroup {
  key: string;
  title: string;
  order: number;
  items: CommandItem[];
}
