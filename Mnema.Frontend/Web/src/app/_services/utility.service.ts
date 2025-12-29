import {HostListener, Injectable, signal} from '@angular/core';

export enum Breakpoint {
  Mobile = 768,
  Tablet = 1280,
  Desktop = 1440
}

@Injectable({
  providedIn: 'root'
})
export class UtilityService {

  private readonly normalizeRegex = /[^\p{L}0-9+!＊！＋]/gu;

  private readonly _breakPoint = signal(Breakpoint.Desktop);
  public readonly breakPoint = this._breakPoint.asReadonly();

  normalize(s: string): string {
    return s.replace(this.normalizeRegex, '').trim().toLowerCase();
  }

  updateBreakPoint() {
    this._breakPoint.set(this.getActiveBreakpoint());
  }

  getActiveBreakpoint(): Breakpoint {
    if (window.innerWidth <= Breakpoint.Mobile) return Breakpoint.Mobile;
    else if (window.innerWidth > Breakpoint.Mobile && window.innerWidth <= Breakpoint.Tablet) return Breakpoint.Tablet;
    else if (window.innerWidth > Breakpoint.Tablet) return Breakpoint.Desktop

    return Breakpoint.Desktop;
  }

}
