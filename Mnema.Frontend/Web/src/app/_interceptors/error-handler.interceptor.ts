import {HttpInterceptorFn} from '@angular/common/http';
import {catchError} from "rxjs";
import {inject} from "@angular/core";
import {ToastrService} from "ngx-toastr";

export const errorHandlerInterceptor: HttpInterceptorFn = (req, next) => {
  const toastr = inject(ToastrService);

  return next(req).pipe(
    catchError(err => {

      switch (err.status) {
        case 404:
          //toastr.warning("Something was not found");
          break;
        case 403:
          toastr.error("You're not allowed to do this!");
          break
        case 500:
          toastr.error("Something went wrong!");
          break;
        case 501:
          toastr.warning("Not implemented yet!", "The feature you're trying to use is not available yet");
          break;
        case 401:
          window.location.href = "/Auth/logout";
          break;
      }

      throw err;
    }),
  );
};
