import {inject, Injectable} from '@angular/core';
import {environment} from "@env/environment";
import {HttpClient} from "@angular/common/http";
import {FormControlDefinition, FormDefinition} from "@mnema/generic-form/form";
import {of, tap} from "rxjs";
import {PagedList} from "@mnema/_models/paged-list";

export interface AuthKey {
  id: string;
  name: string
  key: string;
  roles: string[];
}

@Injectable({
  providedIn: 'root',
})
export class AuthKeyService {

  baseUrl = environment.apiUrl;

  private readonly httpClient = inject(HttpClient);
  private formCache: FormDefinition | undefined;

  all(pageNumber: number, pageSize: number) {
    return this.httpClient.get<PagedList<AuthKey>>(this.baseUrl + 'AuthKey?pageNumber=' + pageNumber + '&pageSize=' + pageSize);
  }

  delete(id: string) {
    return this.httpClient.delete(this.baseUrl + 'AuthKey/' + id);
  }

  update(key: AuthKey) {
    return this.httpClient.put(this.baseUrl + 'AuthKey', key);
  }

  create(key: AuthKey) {
    return this.httpClient.post(this.baseUrl + 'AuthKey', key);
  }

  form() {
    if (!!this.formCache) return of(this.formCache);

    return this.httpClient.get<FormDefinition>(this.baseUrl + 'AuthKey/form').pipe(
      tap(form => this.formCache = form)
    );
  }

}
