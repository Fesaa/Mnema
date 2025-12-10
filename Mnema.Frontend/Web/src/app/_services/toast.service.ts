import {inject, Injectable} from '@angular/core';
import {TranslocoService} from "@jsverse/transloco";
import {ToastrService} from "ngx-toastr";

@Injectable({
  providedIn: 'root'
})
export class ToastService {

  private readonly loco = inject(TranslocoService);
  private readonly toastr = inject(ToastrService);


  infoLoco(key: string, titleValues?: any, summaryValues?: any) {
    this.info(
      this.loco.translate(key+".summary", summaryValues),
      this.loco.translate(key+".title", titleValues),
    )
  }

  private info(title: string, message?: string) {
    this.toastr.info(title, message);
  }

  successLoco(key: string, titleValues?: any, summaryValues?: any) {
    this.success(
      this.loco.translate(key+".summary", summaryValues),
      this.loco.translate(key+".title", titleValues),
    )
  }

  private success(title: string, message?: string) {
    this.toastr.success(title, message);
  }

  warningLoco(key: string, titleValues?: any, summaryValues?: any) {
    this.warning(
      this.loco.translate(key+".summary", summaryValues),
      this.loco.translate(key+".title", titleValues),
    )
  }

  private warning(title: string, message?: string) {
    this.toastr.warning(title, message);
  }

  genericError(msg: string) {
    this.errorLoco("shared.toasts.generic-error", {}, {msg: msg});
  }

  errorLoco(key: string, titleValues?: any, summaryValues?: any) {
    this.error(
      this.loco.translate(key+".summary", summaryValues),
      this.loco.translate(key+".title", titleValues),
    )
  }

  private error(title: string, message?: string) {
    console.debug(`An error occurred${title}:\n ${message}`);
    this.toastr.error(title, message);
  }

}
