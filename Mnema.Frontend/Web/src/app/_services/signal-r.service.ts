import {Injectable} from '@angular/core';
import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {environment} from "../../environments/environment";
import {User} from "../_models/user";
import {ReplaySubject} from "rxjs";

export enum EventType {
  ContentInfoUpdate = "ContentInfoUpdate",
  ContentSizeUpdate = "ContentSizeUpdate",
  ContentProgressUpdate = "ContentProgressUpdate",
  ContentStateUpdate = "ContentStateUpdate",
  AddContent = "AddContent",
  DeleteContent = "DeleteContent",
  Notification = "Notification",
  NotificationRead = "NotificationRead",
  NotificationAdd = "NotificationAdd",
  BulkContentInfoUpdate = "BulkContentInfoUpdate",
  MetadataRefreshed= "MetadataRefreshed",
}

export interface Event<T> {
  type: EventType;
  data: T;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  baseUrl = environment.apiUrl;
  private hubConnection!: HubConnection;

  private eventsSource = new ReplaySubject<Event<any>>(1);

  public events$ = this.eventsSource.asObservable();

  constructor() {

  }

  stopConnection() {
    if (!this.hubConnection) return Promise.resolve();
    return this.hubConnection.stop();
  }

  startConnection(user: User) {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.baseUrl.substring(0, this.baseUrl.length - "api/".length) + "ws", {
        accessTokenFactory: () => user.oidcToken ?? user.token
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .catch((error) => {
        console.error('Error connecting to SignalR hub:', error);
      });

    this.hubConnection.on(EventType.ContentSizeUpdate, (message) => {
      this.eventsSource.next({
        type: EventType.ContentSizeUpdate,
        data: message
      });
    });

    this.hubConnection.on(EventType.ContentProgressUpdate, (message) => {
      this.eventsSource.next({
        type: EventType.ContentProgressUpdate,
        data: message,
      });
    });

    this.hubConnection.on(EventType.AddContent, (message) => {
      this.eventsSource.next({
        type: EventType.AddContent,
        data: message
      });
    });

    this.hubConnection.on(EventType.DeleteContent, (message) => {
      this.eventsSource.next({
        type: EventType.DeleteContent,
        data: message
      });
    });

    this.hubConnection.on(EventType.ContentStateUpdate, (message) => {
      this.eventsSource.next({
        type: EventType.ContentStateUpdate,
        data: message
      });
    });

    this.hubConnection.on(EventType.Notification, (message) => {
      this.eventsSource.next({
        type: EventType.Notification,
        data: message
      });
    });

    this.hubConnection.on(EventType.NotificationAdd, (message) => {
      this.eventsSource.next({
        type: EventType.NotificationAdd,
        data: message
      });
    });

    this.hubConnection.on(EventType.NotificationRead, (message) => {
      this.eventsSource.next({
        type: EventType.NotificationRead,
        data: message
      });
    });

    this.hubConnection.on(EventType.ContentInfoUpdate, (message) => {
      this.eventsSource.next({
        type: EventType.ContentInfoUpdate,
        data: message
      });
    });

    this.hubConnection.on(EventType.BulkContentInfoUpdate, (message) => {
      this.eventsSource.next({
        type: EventType.BulkContentInfoUpdate,
        data: message
      });
    });

    this.hubConnection.on(EventType.MetadataRefreshed, (message) => {
      this.eventsSource.next({
        type: EventType.MetadataRefreshed,
        data: message
      });
    });
  }
}
