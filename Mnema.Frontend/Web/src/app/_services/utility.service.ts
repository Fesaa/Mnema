import {Injectable} from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UtilityService {

  private readonly normalizeRegex = /[^\p{L}0-9+!＊！＋]/gu;

  normalize(s: string): string {
    return s.replace(this.normalizeRegex, '').trim().toLowerCase();
  }

}
