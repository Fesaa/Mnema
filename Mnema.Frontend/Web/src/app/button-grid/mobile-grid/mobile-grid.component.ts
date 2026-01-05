import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonGroup } from '../button-group.service';
import { ButtonGridComponent } from '../button-grid.component';
import { animate, style, transition, trigger } from '@angular/animations';
import { TranslocoModule } from '@jsverse/transloco';

const drawerAnimation = trigger('drawerAnimation', [
  transition(':enter', [
    style({ transform: 'translateY(100%)', opacity: 0 }),
    animate('250ms ease-out', style({ transform: 'translateY(0)', opacity: 1 })),
  ]),
  transition(':leave', [
    animate('200ms ease-in', style({ transform: 'translateY(100%)', opacity: 0 })),
  ]),
]);

const fadeAnimation = trigger('fadeAnimation', [
  transition(':enter', [
    style({ opacity: 0 }),
    animate('200ms ease-out', style({ opacity: 1 })),
  ]),
  transition(':leave', [
    animate('150ms ease-in', style({ opacity: 0 })),
  ]),
]);

@Component({
  selector: 'app-mobile-grid',
  standalone: true,
  imports: [CommonModule, ButtonGridComponent, TranslocoModule],
  templateUrl: './mobile-grid.component.html',
  styleUrls: ['./mobile-grid.component.scss'],
  animations: [drawerAnimation, fadeAnimation],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MobileGridComponent {
  title = input<string>();
  groups = input.required<ButtonGroup[]>();
  columns = input<number>(2);

  close = output<void>();

  onClose() {
    this.close.emit();
  }
}
