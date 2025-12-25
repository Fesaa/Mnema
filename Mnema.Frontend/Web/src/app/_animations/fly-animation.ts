import {animate, style, transition, trigger} from "@angular/animations";

export const flyInOutAnimation = trigger('flyInOut', [
  transition(':enter', [
    style({transform: 'translateX(-100%)'}),
    animate('300ms ease-out', style({transform: 'translateX(0)'}))
  ]),
  transition(':leave', [
    style({transform: 'translateX(0)'}),
    animate('300ms ease-in', style({transform: 'translateX(100%)'}))
  ])]);
