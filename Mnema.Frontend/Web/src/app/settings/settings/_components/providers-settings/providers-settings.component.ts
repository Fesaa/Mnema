import {ChangeDetectionStrategy, Component} from '@angular/core';
import {TranslocoDirective} from '@jsverse/transloco';
import {
  MetadataProviderSettingsComponent
} from '../metadata-provider-settings/metadata-provider-settings.component';
import {ProviderSettingsComponent} from '../provider-settings/provider-settings.component';

@Component({
  selector: 'app-providers-settings',
  imports: [
    TranslocoDirective,
    MetadataProviderSettingsComponent,
    ProviderSettingsComponent,
  ],
  templateUrl: './providers-settings.component.html',
  styleUrl: './providers-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProvidersSettingsComponent {
}
