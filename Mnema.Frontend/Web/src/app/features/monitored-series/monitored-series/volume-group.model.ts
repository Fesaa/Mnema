import {MonitoredChapter} from '@mnema/features/monitored-series/monitored-series.service';

export interface VolumeGroup {
  key: string;
  volume: string;
  hasVolume: boolean;
  chapters: MonitoredChapter[];
  availableChapters: MonitoredChapter[];
  combineChapters: MonitoredChapter[];
  missingCount: number;
  upcomingCount: number;
  coverChapter: MonitoredChapter | null;
}
