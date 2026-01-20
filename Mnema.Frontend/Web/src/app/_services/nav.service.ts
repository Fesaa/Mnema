import {Injectable} from '@angular/core';
import {ReplaySubject} from "rxjs";
import {ActivatedRoute} from "@angular/router";
import {toSignal} from "@angular/core/rxjs-interop";

@Injectable({
  providedIn: 'root'
})
export class NavService {

  private showNavSource = new ReplaySubject<Boolean>(1);
  public showNav$ = this.showNavSource.asObservable();
  public showNav = toSignal(this.showNav$);

  private pageIdSource = new ReplaySubject<string | null>(1);
  public pageId$ = this.pageIdSource.asObservable();
  public readonly pageIndex = toSignal(this.pageId$);

  constructor(private route: ActivatedRoute) {
    this.showNavSource.next(false);

    this.route.queryParams.subscribe(params => {
      const id = params['id'];
      if (id) {
        this.pageIdSource.next(id);
      } else {
        this.pageIdSource.next(null);
      }
    })
  }

  setNavVisibility(show: Boolean) {
    this.showNavSource.next(show);
  }
}
