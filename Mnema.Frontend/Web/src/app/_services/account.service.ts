import {DestroyRef, inject, Injectable, signal} from '@angular/core';
import {environment} from "../../environments/environment";
import {Observable, ReplaySubject, tap} from "rxjs";
import {User, UserDto} from "../_models/user";
import {HttpClient, HttpHeaders} from "@angular/common/http";
import {Router} from "@angular/router";
import {takeUntilDestroyed, toSignal} from "@angular/core/rxjs-interop";
import {PasswordReset} from "../_models/password_reset";
import {SignalRService} from "./signal-r.service";

@Injectable({
  providedIn: 'root'
})
export class AccountService {

  private readonly httpClient = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly signalR = inject(SignalRService);

  baseUrl = environment.apiUrl;

  private readonly _currentUser = signal<User | undefined>(undefined);
  public readonly currentUser = this._currentUser.asReadonly();

  login(model: { username: string, password: string, remember: boolean }): Observable<User> {
    return this.httpClient.post<User>(this.baseUrl + 'login', model).pipe(
      tap((user: User) => {
        if (user) {
          this.setCurrentUser(user)
        }
      }),
      takeUntilDestroyed(this.destroyRef)
    );
  }

  getMe() {
    return this.httpClient.get<User>(this.baseUrl+"user/me").pipe(
      tap((user) => {
        this.setCurrentUser(user);
      }),
      takeUntilDestroyed(this.destroyRef)
    );
  }

  updateMe(model: {username: string, email: string}) {
    return this.httpClient.post<User>(`${this.baseUrl}user/me`, model).pipe(
      tap(() => {
        this._currentUser.update(x => {
          if (!x) return;

          x.name = model.username;
          x.email = model.email;
          return x;
        })
      }),
    );
  }

  updatePassword(model: {oldPassword: string, newPassword: string}) {
    return this.httpClient.post(`${this.baseUrl}user/password`, model)
  }

  register(model: { username: string, password: string, remember: boolean }): Observable<User> {
    return this.httpClient.post<User>(this.baseUrl + 'register', model).pipe(
      tap((user: User) => {
        if (user) {
          this.setCurrentUser(user);
        }
      }),
      takeUntilDestroyed(this.destroyRef)
    );
  }

  setCurrentUser(user?: User) {
    this._currentUser.set(user);

    if (user) {
      this.signalR.stopConnection()
        .then(() => this.signalR.startConnection(user));
    }
  }

  logout() {
    if (!this.currentUser()) {
      return;
    }

    this._currentUser.set(undefined);
    window.location.href = "/Auth/logout"
  }

  all() {
    return this.httpClient.get<UserDto[]>(this.baseUrl + 'user/all');
  }

  updateOrCreate(dto: UserDto) {
    return this.httpClient.post<UserDto>(this.baseUrl + 'user/update', dto).pipe(tap(dto => {
      this._currentUser.update(user => {
        if (!user || dto.id !== user.id) {
          return;
        }

        user.name = dto.name;
        user.email = dto.email;
        user.roles = dto.roles;
        return user;
      });
    }))
  }

  delete(id: number) {
    return this.httpClient.delete(this.baseUrl + 'user/' + id);
  }

  generateReset(id: number) {
    return this.httpClient.post<PasswordReset>(this.baseUrl + 'user/reset/' + id, {})
  }

  resetPassword(model: { key: string, password: string }) {
    return this.httpClient.post(this.baseUrl + 'reset-password', model)
  }

  refreshApiKey() {
    return this.httpClient.get<{ ApiKey: string }>(this.baseUrl + 'user/refresh-api-key').pipe(tap(res => {
      this._currentUser.update(x => {
        if (!x) return

        x.apiKey = res.ApiKey;
        return x;
      })
    }));
  }

}
