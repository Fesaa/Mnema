import {Provider} from './page';
import {MetadataProvider} from '@mnema/features/monitored-series/metadata.service';

export interface ParsedSeriesUrl {
  provider: Provider | null;
  metadataProvider: MetadataProvider | null;
  externalId: string;
  seriesName: string;
  description: string | null;
  coverUrl: string | null;
}
