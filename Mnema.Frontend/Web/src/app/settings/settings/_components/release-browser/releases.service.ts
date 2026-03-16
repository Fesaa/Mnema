import {inject, Injectable} from '@angular/core';
import {environment} from "@env/environment";
import {HttpClient} from "@angular/common/http";
import {PagedList} from "@mnema/_models/paged-list";
import {ContentReleaseDto} from "@mnema/settings/settings/_components/release-browser/release";

@Injectable({
  providedIn: 'root',
})
export class ReleasesService {

  private readonly httpClient = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getGrabbedReleases(pageNumber: number, pageSize: number, query?: string) {
    return this.httpClient.get<PagedList<ContentReleaseDto>>(`${this.apiUrl}Releases/releases?pageNumber=${pageNumber}&pageSize=${pageSize}&query=${query ?? ''}`)
  }

  getImportedReleases(pageNumber: number, pageSize: number, query?: string) {
    return this.httpClient.get<PagedList<ContentReleaseDto>>(`${this.apiUrl}Releases/imported?pageNumber=${pageNumber}&pageSize=${pageSize}&query=${query ?? ''}`)
  }

  deleteRelease(id: string) {
    return this.httpClient.delete(`${this.apiUrl}Releases/${id}`);
  }

}
