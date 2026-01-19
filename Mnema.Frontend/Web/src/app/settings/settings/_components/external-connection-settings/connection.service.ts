import {inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {environment} from "../../../../../environments/environment";
import {PagedList} from "../../../../_models/paged-list";
import {FormDefinition} from "../../../../generic-form/form";

export interface Connection {
  id: string;
  name: string;
  type: ConnectionType;
  followedEvents: ConnectionEvent[];
  metadata: { [key: string]: string[] };
}

export enum ConnectionType {
  Discord = 0,
  Kavita = 1,
  Native = 2,
}

export const ConnectionTypes = [ConnectionType.Discord, ConnectionType.Kavita, ConnectionType.Native];

export enum ConnectionEvent {
  DownloadStarted = 0,
  DownloadFinished = 1,
  DownloadFailure = 2,
}

@Injectable({
  providedIn: 'root',
})
export class ConnectionService {

  private readonly httpClient = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getConnections(pageNumber: number, pageSize: number) {
    return this.httpClient.get<PagedList<Connection>>(this.baseUrl + `Connection?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getConnectionForm(type: ConnectionType) {
    return this.httpClient.get<FormDefinition>(this.baseUrl + `Connection/form?type=${type}`);
  }

  updateConnection(connection: Connection) {
    return this.httpClient.post(this.baseUrl + `Connection`, connection);
  }

  deleteConnection(id: string) {
    return this.httpClient.delete(this.baseUrl + `Connection/${id}`);
  }


}
