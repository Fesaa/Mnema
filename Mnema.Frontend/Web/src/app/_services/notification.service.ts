import {Injectable} from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import {environment} from "../../environments/environment";
import {Notification} from "../_models/notifications";
import {PagedList} from "../_models/paged-list";

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private baseUrl = environment.apiUrl + "notifications";

  constructor(private http: HttpClient) {}

  all(pageNumber: number, pageSize: number) {
    const params = new HttpParams().set("pageNumber", pageNumber).set("pageSize", pageSize);

    return this.http.get<PagedList<Notification>>(`${this.baseUrl}/all`, { params });
  }

  recent(limit: number = 5) {
    return this.http.get<Notification[]>(`${this.baseUrl}/recent?limit=${limit}`)
  }

  amount() {
    return this.http.get<number>(`${this.baseUrl}/amount`);
  }

  markAsRead(id: number) {
    return this.http.post<any>(`${this.baseUrl}/${id}/read`, {});
  }

  markAsUnread(id: number) {
    return this.http.post<any>(`${this.baseUrl}/${id}/unread`, {});
  }

  deleteNotification(id: number) {
    return this.http.delete<any>(`${this.baseUrl}/${id}`);
  }

  readMany(ids: number[]) {
    return this.http.post(`${this.baseUrl}/many/read`, ids);
  }

  deleteMany(ids: number[]) {
    return this.http.post(`${this.baseUrl}/many/delete`, ids);
  }
}
