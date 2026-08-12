import { ResolveFn } from '@angular/router';

export const importScanResolver: ResolveFn<boolean> = (route, state) => {
  return true;
};
