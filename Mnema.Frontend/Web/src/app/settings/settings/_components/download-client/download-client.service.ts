import {inject, Injectable} from '@angular/core';
import {MetadataBag} from "../../../../_models/search";
import {HttpClient} from "@angular/common/http";
import {environment} from "../../../../../environments/environment";
import {PagedList} from "../../../../_models/paged-list";
import {FormDefinition} from "../../../../generic-form/form";

export interface DownloadClient {
  id: string;
  name: string;
  isFailed: boolean;
  failedAt: Date;
  type: DownloadClientType,
  metadata: MetadataBag,
}

export enum DownloadClientType {
  QBitTorrent = 0,
}

@Injectable({
  providedIn: 'root',
})
export class DownloadClientService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getDownloadClients(pageNumber: number, pageSize: number) {
    return this.httpClient.get<PagedList<DownloadClient>>(this.baseUrl + `DownloadClient?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getFreeTypes() {
    return this.httpClient.get<DownloadClientType[]>(this.baseUrl + 'DownloadClient/available-types')
  }

  getForm(type: DownloadClientType) {
    return this.httpClient.get<FormDefinition>(this.baseUrl + `DownloadClient/form?type=${type}`);
  }

  updateDownloadClient(downloadClient: DownloadClient) {
    return this.httpClient.post(this.baseUrl + `DownloadClient`, downloadClient);
  }

  deleteDownloadClient(id: string) {
    return this.httpClient.delete(this.baseUrl + `DownloadClient/${id}`);
  }

  releaseLock(id: string) {
    return this.httpClient.delete(this.baseUrl + `DownloadClient/${id}/failed-lock`);
  }


}
