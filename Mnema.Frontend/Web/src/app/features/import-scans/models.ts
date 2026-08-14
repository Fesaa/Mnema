

export interface ImportScan {
  id: string;
  rootDir: string;
  directoryImportResultCount: number;
  importErrorCount: number;
  status: ImportScanStatus;
  startedUtc: string;
  finishedUtc?: string;
  createdUtc: string;
  lastModifiedUtc: string;
}

export enum ImportScanStatus {
  Queued = 0,
  Started = 1,
  Finished = 2,
  Failed = 3,
}

export interface DirectoryImportResult {
  id: string;
  directory: string;
  status: DirectoryImportStatus;
  importScanId: string;
  monitoredSeriesId: string;
  parsedSeriesName: string;
  parsedHardcoverId: number;
  parsedMangaBakaId: number;
  files: string[];
  createdUtc: string;
  lastModifiedUtc: string;
}

export interface UpdateDirectoryImportResult {
  parsedSeriesName: string;
  parsedHardcoverId: number;
  parsedMangaBakaId: number;
}

export enum DirectoryImportStatus {
  Queued = 0,
  Rejected = 1,
  Imported = 2,
}

export interface ImportError {
  id: string;
  path: string;
  message: string;
  stackTrace?: string;
  createdUtc: string;
  lastModifiedUtc: string;
}

export interface StartScanRequest {
  rootDir: string;
}
