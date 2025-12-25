export type Config = {
  maxConcurrentTorrents: number;
  maxConcurrentImages: number;
  rootDir: string;
  subscriptionRefreshHour: number;
  version: string;
  firstInstalledVersion: string;
  installDate: Date;
  lastUpdateDate: Date;
}
