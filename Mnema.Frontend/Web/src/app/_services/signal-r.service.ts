import {effect, inject, Injectable} from '@angular/core';
import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {environment} from "../../environments/environment";
import {ReplaySubject} from "rxjs";
import {SettingsService} from "@mnema/_services/settings.service";

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
  RefreshDashboard = "RefreshDashboard",
}

export interface Event<T> {
  type: EventType;
  data: T;
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {

  private readonly settingsService = inject(SettingsService);

  baseUrl = environment.apiUrl;
  private hubConnection!: HubConnection;

  private eventsSource = new ReplaySubject<Event<any>>(1);

  public events$ = this.eventsSource.asObservable();

  constructor() {
    effect(() => {
      if (this.settingsService.isAuthenticated()) {
        this.startConnection()
      } else {
        this.stopConnection().catch(console.error).then(() => void 0);
      }
    });
  }

  stopConnection() {
    if (!this.hubConnection) return Promise.resolve();
    return this.hubConnection.stop();
  }

  startConnection() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.baseUrl.substring(0, this.baseUrl.length - "api/".length) + "ws")
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
