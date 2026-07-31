import {effect, inject, Injectable} from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {environment} from "../../environments/environment";
import {of, Subject} from "rxjs";
import {ToastService} from "./toast.service";
import {AuthKeyService} from "@mnema/settings/settings/_components/auth-keys/auth-key.service";

@Injectable({
  providedIn: 'root'
})
export class ImageService {

  baseUrl = environment.apiUrl;
  apiKey: string | null = null;

  constructor(private httpClient: HttpClient) {

  }

  getImage(imageUrl: string) {
    if (true) {
      return of(this.baseUrl + imageUrl);
    }

    const imageSrc = new Subject<string>();
    this.httpClient.get(this.baseUrl + imageUrl, {responseType: 'blob'}).subscribe({
      next: blob => {
        const reader = new FileReader();
        reader.onloadend = () => {
          imageSrc.next(reader.result as string);
        }
        reader.readAsDataURL(blob);
      },
    })
    return imageSrc.asObservable();
  }
}
