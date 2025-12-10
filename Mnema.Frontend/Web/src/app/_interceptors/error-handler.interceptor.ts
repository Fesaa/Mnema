import { HttpInterceptorFn } from '@angular/common/http';
import {catchError} from "rxjs";
import {inject} from "@angular/core";
import {ToastrService} from "ngx-toastr";

export const errorHandlerInterceptor: HttpInterceptorFn = (req, next) => {
  const toastr = inject(ToastrService);

  return next(req).pipe(
    catchError(err => {

      switch (err.status) {
        case 404:
          toastr.warning("Something was not found");
          break;
        case 501:
          toastr.warning("Not feature has not been implemented yet")
          break;
      }

      throw new err;
    }),
  );
};
