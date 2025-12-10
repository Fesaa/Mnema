import {animate, keyframes, style, transition, trigger} from "@angular/animations";

export const bounceIn500ms = trigger('bounceIn500ms', [
  transition(':enter', [
    style({opacity: 0, transform: 'translateY(-50px)'}),
    animate('500ms 500ms', keyframes([
      style({opacity: 0, transform: 'translateY(-50px)', offset: 0}),
      style({opacity: 1, transform: 'translateY(0)', offset: 0.6}),
      style({opacity: 1, transform: 'translateY(-10px)', offset: 0.8}),
      style({opacity: 1, transform: 'translateY(0)', offset: 1}),
    ]))
  ])
]);

export const bounceIn200ms = trigger('bounceIn200ms', [
  transition(':enter', [
    style({opacity: 0, transform: 'translateY(-50px)'}),
    animate('200ms 200ms', keyframes([
      style({opacity: 0, transform: 'translateY(-50px)', offset: 0}),
      style({opacity: 1, transform: 'translateY(0)', offset: 0.6}),
      style({opacity: 1, transform: 'translateY(-10px)', offset: 0.8}),
      style({opacity: 1, transform: 'translateY(0)', offset: 1}),
    ]))
  ])
]);
