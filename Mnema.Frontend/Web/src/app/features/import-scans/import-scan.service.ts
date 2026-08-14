import {inject, Service} from '@angular/core';
import {environment} from "@env/environment";
import {HttpClient} from "@angular/common/http";
import {
  DirectoryImportResult,
  ImportError,
  ImportScan,
  StartScanRequest,
  UpdateDirectoryImportResult
} from "@mnema/features/import-scans/models";
import {PagedList} from "@mnema/_models/paged-list";

@Service()
export class ImportScanService {

  apiUrl = environment.apiUrl + 'ImportScan';
  private readonly httpClient = inject(HttpClient);

  startScan(req: StartScanRequest) {
    return this.httpClient.post(this.apiUrl + '/start-import-scan', req);
  }

  getPagedScans(pageNumber: number, pageSize: number) {
    return this.httpClient.get<PagedList<ImportScan>>(this.apiUrl + `/paged?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getById(id: string) {
    return this.httpClient.get<ImportScan>(this.apiUrl + `/${id}`);
  }

  getErrorsPaged(id: string, pageNumber: number, pageSize: number) {
      return this.httpClient.get<PagedList<ImportError>>(this.apiUrl + `/${id}/errors?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getDirectoriesPaged(id: string, pageNumber: number, pageSize: number) {
    return this.httpClient.get<PagedList<DirectoryImportResult>>(this.apiUrl + `/${id}/directories?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  delete(id: string) {
    return this.httpClient.delete(this.apiUrl + `/${id}`);
  }

  rejectDirectoryImportResult(id: string) {
    return this.httpClient.post(this.apiUrl + `/${id}/reject`, {});
  }

  skipDirectoryImportResult(id: string) {
    return this.httpClient.post(this.apiUrl + `/${id}/skip`, {});
  }

  autoAcceptDirectoryImportResult(id: string) {
    return this.httpClient.post(this.apiUrl + `/${id}/auto-accept`, {});
  }

  updateDirectoryImportResult(id: string, req: UpdateDirectoryImportResult) {
    return this.httpClient.post(this.apiUrl + `/${id}/update`, req);
  }

}
