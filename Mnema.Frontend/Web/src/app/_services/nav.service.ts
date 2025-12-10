import {inject, Injectable} from '@angular/core';
import {ReplaySubject} from "rxjs";
import {ActivatedRoute, Router} from "@angular/router";
import {PageService} from "./page.service";
import {toSignal} from "@angular/core/rxjs-interop";

@Injectable({
  providedIn: 'root'
})
export class NavService {

  private pageService = inject(PageService);
  private router = inject(Router);

  private showNavSource = new ReplaySubject<Boolean>(1);
  public showNav$ = this.showNavSource.asObservable();

  private pageIndexSource = new ReplaySubject<number | null>(1);
  public pageIndex$ = this.pageIndexSource.asObservable();
  public readonly pageIndex = toSignal(this.pageIndex$);

  constructor(private route: ActivatedRoute) {
    this.showNavSource.next(false);

    this.route.queryParams.subscribe(params => {
      const index = params['index'];
      if (index) {
        try {
          this.pageIndexSource.next(index)
        } catch (e) {
          console.error(e);
        }
      } else {
        this.pageIndexSource.next(null);
      }
    })
  }

  setNavVisibility(show: Boolean) {
    this.showNavSource.next(show);
  }
}
