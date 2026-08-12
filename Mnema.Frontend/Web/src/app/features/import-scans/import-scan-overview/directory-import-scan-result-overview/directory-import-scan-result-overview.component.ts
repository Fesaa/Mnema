import {Component, input} from '@angular/core';
import {DirectoryImportResult, DirectoryImportStatus} from "@mnema/features/import-scans/models";

@Component({
  selector: 'app-directory-import-scan-result-overview',
  imports: [],
  templateUrl: './directory-import-scan-result-overview.component.html',
  styleUrl: './directory-import-scan-result-overview.component.scss',
})
export class DirectoryImportScanResultOverviewComponent {

  result = input.required<DirectoryImportResult>();

}
