import {inject, Injectable, TemplateRef, Type} from '@angular/core';
import {NgbModal, NgbModalOptions, NgbModalRef} from '@ng-bootstrap/ng-bootstrap';
import {DefaultModalOptions} from '../_models/default-modal-options';
import {filter, firstValueFrom, from, map, take, takeUntil} from 'rxjs';
import {ConfirmModalComponent} from "../shared/_component/confirm-modal/confirm-modal.component";
import {DirectorySelectorComponent} from "../shared/_component/directory-selector/directory-selector.component";


@Injectable({
  providedIn: 'root'
})
export class ModalService {

  private modal = inject(NgbModal);

  open<T>(content: Type<T>, options?: NgbModalOptions): [NgbModalRef, T]  {
    const modal = this.modal.open(content, options);
    return [modal, modal.componentInstance as T]
  }

  onClose$<T>(modal: NgbModalRef) {
    return modal.closed.pipe(
      takeUntil(modal.dismissed),
      take(1),
      map(obj => obj as T)
    );
  }

  hasOpenModals() {
    return this.modal.hasOpenModals()
  }

  get activeInstances() {
    return this.modal.activeInstances
  }

  dismissAll(reason?: any) {
    this.modal.dismissAll(reason);
  }

  getDirectory(
    root: string,
    options: Partial<{ create: boolean; copy: boolean; filter: boolean; showFiles: boolean, width: string }> = {}
  ) {
    const defaultOptions = {create: false, copy: false, filter: true, showFiles: false, width: '50vw'};
    const finalOptions = {...defaultOptions, ...options};

    const [modal, component] = this.open(DirectorySelectorComponent, DefaultModalOptions);
    component.root = root;
    component.filter = finalOptions.filter;
    component.copy = finalOptions.copy;
    component.create = finalOptions.create;
    component.showFiles = finalOptions.showFiles;
    component.customWidth = finalOptions.width

    return new Promise<string | undefined>((resolve, reject) => {

      let hasResolved = false;
      component.getResult().subscribe(result => {
        resolve(result);
        hasResolved = true;
      });

      modal.result.finally(() => {
        if (!hasResolved) resolve(undefined)
      });
    });
  }

  confirm(options: {
    question: string;
    title?: string;
    bodyTemplate?: TemplateRef<unknown>;
    templateData?: unknown;
  }) {
    return firstValueFrom(this.confirm$(options))
  }

  confirm$(options: {
    question: string;
    title?: string;
    bodyTemplate?: TemplateRef<unknown>;
    templateData?: unknown;
  }, onlyEmitTrue: boolean = false) {
    const [_, component] = this.open(ConfirmModalComponent, DefaultModalOptions);

    component.question.set(options.question);

    if (options.title) {
      component.title.set(options.title);
    }

    if (options.bodyTemplate) {
      component.bodyTemplate.set(options.bodyTemplate);
    }

    if (options.templateData) {
      component.templateData.set(options.templateData);
    }

    return component.result$.pipe(filter(b => !onlyEmitTrue || b), take(1));
  }


}
