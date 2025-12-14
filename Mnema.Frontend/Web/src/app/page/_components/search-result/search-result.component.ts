import {Component, inject, input, OnInit, signal} from '@angular/core';
import {SearchInfo} from "../../../_models/Info";
import {DownloadMetadata, Page, Provider} from "../../../_models/page";
import {bounceIn200ms} from "../../../_animations/bounce-in";
import {dropAnimation} from "../../../_animations/drop-animation";
import {ImageService} from "../../../_services/image.service";
import {TranslocoDirective} from "@jsverse/transloco";
import {NgStyle} from "@angular/common";
import {NgbTooltip} from "@ng-bootstrap/ng-bootstrap";
import {ModalService} from "../../../_services/modal.service";
import {DownloadModalComponent} from "../download-modal/download-modal.component";
import {DefaultModalOptions} from "../../../_models/default-modal-options";
import {
  EditSubscriptionModalComponent
} from "../../../subscription-manager/_components/edit-subscription-modal/edit-subscription-modal.component";
import {RefreshFrequency, Subscription} from "../../../_models/subscription";

@Component({
  selector: 'app-search-result',
  imports: [
    TranslocoDirective,
    NgStyle,
    NgbTooltip,
  ],
  templateUrl: './search-result.component.html',
  styleUrl: './search-result.component.scss',
  animations: [bounceIn200ms, dropAnimation]
})
export class SearchResultComponent implements OnInit{

  private readonly imageService = inject(ImageService);
  private readonly modalService = inject(ModalService);

  page = input.required<Page>();
  searchResult = input.required<SearchInfo>();
  providers = input.required<Provider[]>();
  metadata = input.required<DownloadMetadata>();

  imageSource = signal<string | null>(null);


  ngOnInit(): void {
    this.loadImage();
  }

  addAsSub() {
    const [_, component] = this.modalService.open(EditSubscriptionModalComponent, DefaultModalOptions);

    const newSub: Subscription = {
      ID: -1,
      contentId: this.searchResult().id,
      provider: this.searchResult().provider,
      refreshFrequency: RefreshFrequency.Week,
      title: this.searchResult().name,
      baseDir: this.page().dirs[0],
      lastDownloadDir: '',
      lastCheck: null!,
      lastCheckSuccess: null!,
      nextExecution: null!,
      metadata: {
        startImmediately: true,
        extra: {}
      }
    };

    component.subscription.set(newSub);
    component.metadata.set(this.metadata());
    component.providers.set(this.providers());
  }

  download() {
    const metadata = this.metadata();
    if (!metadata) return

    const page = this.page();
    const defaultDir = (page.dirs.length === 0 ? '' : page.dirs[0]) || page.customRootDir;

    const [_, component] = this.modalService.open(DownloadModalComponent, DefaultModalOptions);
    component.metadata.set(metadata);
    component.defaultDir.set(defaultDir);
    component.rootDir.set(page.customRootDir);
    component.dirs.set(page.dirs);
    component.info.set(this.searchResult());
  }

  loadImage() {
    if (this.searchResult().imageUrl === "") {
      return;
    }

    if (this.searchResult().imageUrl.startsWith("proxy")) {
      this.imageService.getImage(this.searchResult().imageUrl).subscribe(src => {
        this.imageSource.set(src);
      })
    } else {
      this.imageSource.set(this.searchResult().imageUrl);
    }
  }

}
