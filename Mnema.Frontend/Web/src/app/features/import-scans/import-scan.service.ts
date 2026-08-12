import {inject, Service} from '@angular/core';
import {environment} from "@env/environment";
import {HttpClient} from "@angular/common/http";
import {FullImportScan, ShallowImportScan, StartScanRequest} from "@mnema/features/import-scans/models";
import {PagedList} from "@mnema/_models/paged-list";

@Service()
export class ImportScanService {

  apiUrl = environment.apiUrl + 'ImportScan';
  private readonly httpClient = inject(HttpClient);

  startScan(req: StartScanRequest) {
    return this.httpClient.post(this.apiUrl + '/start-import-scan', req);
  }

  getPagedScans(pageNumber: number, pageSize: number) {
    return this.httpClient.get<PagedList<ShallowImportScan>>(this.apiUrl + `/paged?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getById(id: string) {
    return this.httpClient.get<FullImportScan>(this.apiUrl + `/${id}`);
  }

  delete(id: string) {
    return this.httpClient.delete(this.apiUrl + `/${id}`);
  }

}
