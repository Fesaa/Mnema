import {inject, Injectable} from '@angular/core';
import {TranslocoService} from "@jsverse/transloco";
import {RetoastService} from "ngx-retoast";

@Injectable({
  providedIn: 'root'
})
export class ToastService {

  private readonly loco = inject(TranslocoService);
  private readonly toastr = inject(RetoastService);


  infoLoco(key: string, titleValues?: any, summaryValues?: any) {
    return this.info(
      this.loco.translate(key+".summary", summaryValues),
      this.loco.translate(key+".title", titleValues),
    )
  }

  private info(title: string, message?: string) {
    return this.toastr.info(title, message);
  }

  successLoco(key: string, titleValues?: any, summaryValues?: any) {
    return this.success(
      this.loco.translate(key+".summary", summaryValues),
      this.loco.translate(key+".title", titleValues),
    )
  }

  private success(title: string, message?: string) {
    return this.toastr.success(title, message);
  }

  warningLoco(key: string, titleValues?: any, summaryValues?: any) {
    return this.warning(
      this.loco.translate(key+".summary", summaryValues),
      this.loco.translate(key+".title", titleValues),
    )
  }

  private warning(title: string, message?: string) {
    return this.toastr.warning(title, message);
  }

  genericError(msg: string) {
    return this.errorLoco("shared.toasts.generic-error", {}, {msg: msg});
  }

  errorLoco(key: string, titleValues?: any, summaryValues?: any) {
    return this.error(
      this.loco.translate(key+".summary", summaryValues),
      this.loco.translate(key+".title", titleValues),
    )
  }

  private error(title: string, message?: string) {
    console.debug(`An error occurred${title}:\n ${message}`);
    return this.toastr.error(title, message);
  }

}
