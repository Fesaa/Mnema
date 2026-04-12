import {DestroyRef, inject, Injectable, signal} from '@angular/core';
import {environment} from "@env/environment";
import {tap} from "rxjs";
import {User, UserDto} from "../_models/user";
import {HttpClient} from "@angular/common/http";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
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

  getMe() {
    return this.httpClient.get<User>(this.baseUrl+"user/me").pipe(
      tap((user) => {
        this.setCurrentUser(user);
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
  delete(id: number) {
    return this.httpClient.delete(this.baseUrl + 'user/' + id);
  }
}
