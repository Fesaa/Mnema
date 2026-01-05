import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

@Component({
  selector: 'app-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span
      [class]="badgeClass()"
      role="status"
    >
      <ng-content></ng-content>
    </span>
  `,
  styleUrl: `./badge.component.scss`,
})
export class BadgeComponent {

  colour = input<'primary' | 'secondary' | 'error' | 'warning'>('primary');
  padding = input('');

  badgeClass = computed(() => `badge badge--${this.colour()} ${this.padding()}`);

}
