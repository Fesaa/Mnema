import {ChangeDetectionStrategy, Component} from '@angular/core';
import {TranslocoDirective} from '@jsverse/transloco';
import {
  ConnectionSettingsComponent
} from '../external-connection-settings/connection-settings.component';
import {DownloadClientSettingsComponent} from '../download-client/download-client-settings.component';

@Component({
  selector: 'app-integrations-settings',
  imports: [
    TranslocoDirective,
    ConnectionSettingsComponent,
    DownloadClientSettingsComponent,
  ],
  templateUrl: './integrations-settings.component.html',
  styleUrl: './integrations-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IntegrationsSettingsComponent {
}
