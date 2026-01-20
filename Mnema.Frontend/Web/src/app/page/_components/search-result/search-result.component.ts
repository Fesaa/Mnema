import {Component, inject, input, OnInit, signal} from '@angular/core';
import {SearchInfo} from "../../../_models/Info";
import {Page, Provider} from "../../../_models/page";
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
import {Subscription, SubscriptionStatus} from "../../../_models/subscription";
import {SeriesInfoComponent} from "../series-info/series-info.component";
import {FormControlDefinition} from "../../../generic-form/form";
import {SeriesService} from "@mnema/page/_components/series-info/series.service";
import {tap} from "rxjs";

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
  private readonly seriesService = inject(SeriesService);

  page = input.required<Page>();
  searchResult = input.required<SearchInfo>();
  providers = input.required<Provider[]>();
  metadata = input.required<FormControlDefinition[]>();

  imageSource = signal<string | null>(null);


  ngOnInit(): void {
    this.loadImage();
  }

  addAsSub() {
    const [_, component] = this.modalService.open(EditSubscriptionModalComponent, DefaultModalOptions);

    const newSub: Subscription = {
      id: '',
      contentId: this.searchResult().id,
      provider: this.searchResult().provider,
      title: this.searchResult().name,
      baseDir: this.page().customRootDir,
      metadata: {},
      status: SubscriptionStatus.Enabled,
    };

    component.subscription.set(newSub);
    component.metadata.set(this.metadata());
    component.providers.set(this.providers());
  }

  download() {
    const metadata = this.metadata();
    if (!metadata) return

    const page = this.page();
    const defaultDir = page.customRootDir;

    const [_, component] = this.modalService.open(DownloadModalComponent, DefaultModalOptions);
    component.metadata.set(metadata);
    component.defaultDir.set(defaultDir);
    component.rootDir.set(page.customRootDir);
    component.info.set(this.searchResult());
  }

  loadInfo() {
    this.seriesService.getSeriesInfo(this.searchResult().provider, this.searchResult().id).pipe(
      tap(series => {
        const [_, component] = this.modalService.open(SeriesInfoComponent, DefaultModalOptions);
        component.series.set(series);
      }),
    ).subscribe();
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
