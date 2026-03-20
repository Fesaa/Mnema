import {ChangeDetectionStrategy, Component, computed, EventEmitter, inject, signal} from '@angular/core';
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {TableComponent} from "@mnema/shared/_component/table/table.component";
import {ReleasesService} from "@mnema/settings/settings/_components/release-browser/releases.service";
import {PageLoader} from "@mnema/shared/_component/paginator/paginator.component";
import {ContentReleaseDto} from "@mnema/settings/settings/_components/release-browser/release";
import {DatePipe} from "@angular/common";
import {DefaultValuePipe} from "@mnema/_pipes/default-value.pipe";
import {ModalService} from "@mnema/_services/modal.service";
import {debounce, form, FormField} from "@angular/forms/signals";

@Component({
  selector: 'app-release-browser',
  imports: [
    TranslocoDirective,
    TableComponent,
    DatePipe,
    DefaultValuePipe,
    FormField
  ],
  templateUrl: './release-browser.component.html',
  styleUrl: './release-browser.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReleaseBrowserComponent {

  private readonly releasesService = inject(ReleasesService);
  private readonly modalService = inject(ModalService);

  protected readonly imported = signal(false);
  private readonly query = signal('');
  protected readonly queryForm = form(this.query, schemaPath => {
    debounce(schemaPath, 200);
  });

  protected pageReloader = new EventEmitter<void>();
  protected readonly pageLoader = computed<PageLoader<ContentReleaseDto>>(() => {
    const imported = this.imported();
    const query = this.query();

    return (pageNumber: number, pageSize: number) => {
      return imported
        ? this.releasesService.getImportedReleases(pageNumber, pageSize, query)
        : this.releasesService.getGrabbedReleases(pageNumber, pageSize, query);
    }
  });

  trackBy(_: number, release: ContentReleaseDto) {
    return release.id;
  }

  toggle() {
    this.imported.update(x => !x);
  }

  async delete(release: ContentReleaseDto) {

    if (!await this.modalService.confirm({
      question: translate('settings.releases.confirm-delete', {id: release.releaseId})
    })) return;

    this.releasesService.deleteRelease(release.id).subscribe(() => this.pageReloader.emit());
  }



}
