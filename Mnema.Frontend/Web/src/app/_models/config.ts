
export type Config = {
  maxConcurrentImages: number;
  subscriptionRefreshHour: number;
  version: string;
  firstInstalledVersion: string;
  installDate: Date;
  lastUpdateDate: Date;
  autoDisableProviderAfter: number;
  imageConversionLossless: boolean;
  imageConversionQuality: number;
}

export type UpdateServerSettings = {
  maxConcurrentImages: number;
  autoDisableProviderAfter: number;
  imageConversionLossless: boolean;
  imageConversionQuality: number;
}
