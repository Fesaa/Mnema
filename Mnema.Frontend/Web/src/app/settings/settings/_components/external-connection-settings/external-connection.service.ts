import {inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {environment} from "../../../../../environments/environment";
import {PagedList} from "../../../../_models/paged-list";
import {FormDefinition} from "../../../../generic-form/form";

export interface ExternalConnection {
  id: string;
  name: string;
  type: ExternalConnectionType;
  followedEvents: ExternalConnectionEvent[];
  metadata: { [key: string]: string[] };
}

export enum ExternalConnectionType {
  Discord = 0,
  Kavita = 1,
}

export const ExternalConnectionTypes = [ExternalConnectionType.Discord, ExternalConnectionType.Kavita];

export enum ExternalConnectionEvent {
  DownloadStarted = 0,
  DownloadFinished = 1,
  DownloadFailure = 2,
}

@Injectable({
  providedIn: 'root',
})
export class ExternalConnectionService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getExternalConnections(pageNumber: number, pageSize: number) {
    return this.httpClient.get<PagedList<ExternalConnection>>(this.baseUrl + `ExternalConnection?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getConnectionForm(type: ExternalConnectionType) {
    return this.httpClient.get<FormDefinition>(this.baseUrl + `ExternalConnection/form?type=${type}`);
  }

  updateExternalConnection(externalConnection: ExternalConnection) {
    return this.httpClient.post(this.baseUrl + `ExternalConnection`, externalConnection);
  }

  deleteExternalConnection(id: string) {
    return this.httpClient.delete(this.baseUrl + `ExternalConnection/${id}`);
  }


}
