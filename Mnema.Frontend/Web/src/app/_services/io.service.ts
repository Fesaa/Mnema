import {Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {CreateDirRequest, DirEntry, ListDirRequest} from "../_models/io";
import {environment} from "../../environments/environment";

@Injectable({
  providedIn: 'root'
})
export class IoService {

  baseUrl = environment.apiUrl

  constructor(private httpClient: HttpClient) {
  }

  ls(dir: string, showFiles: boolean = false) {
    const req: ListDirRequest = {dir, files: showFiles};
    return this.httpClient.post<DirEntry[]>(this.baseUrl + 'io/ls', req);
  }

  create(baseDir: string, newDir: string) {
    const req: CreateDirRequest = {baseDir, newDir};
    return this.httpClient.post(this.baseUrl + 'io/create', req, {responseType: 'text'});
  }

}
