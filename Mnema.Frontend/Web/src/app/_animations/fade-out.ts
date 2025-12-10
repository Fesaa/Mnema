import {animate, style, transition, trigger} from "@angular/animations";

export const fadeOut = trigger('fadeAnimation', [
  transition(':enter', [
    style({opacity: 0}),
    animate('500ms ease-out', style({opacity: 1}))
  ]),
  transition(':leave', [
    style({opacity: 1}),
    animate('500ms ease-in', style({opacity: 0}))
  ])]);
