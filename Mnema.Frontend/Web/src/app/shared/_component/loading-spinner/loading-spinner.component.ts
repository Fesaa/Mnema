import {Component, input, ChangeDetectionStrategy} from '@angular/core';


type SpinnerSize = 'small' | 'medium' | 'large';
type SpinnerColour = 'primary' | 'secondary' | 'white';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [],
  templateUrl: './loading-spinner.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./loading-spinner.component.scss']
})
export class LoadingSpinnerComponent {
  size  = input<SpinnerSize>('medium');
  colour = input<SpinnerColour>('primary');
  centered = input(false);
  message = input('');
}
