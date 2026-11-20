import {inject, Injectable} from '@angular/core';
import {NgbModalOptions} from '@ng-bootstrap/ng-bootstrap';
import {ModalService} from '@mnema/_services/modal.service';
import {CommandPaletteComponent} from './command-palette.component';

const COMMAND_PALETTE_MODAL_OPTIONS: NgbModalOptions = {
  windowClass: 'command-palette-modal',
  backdropClass: 'command-palette-backdrop',
  size: 'lg',
  centered: false,
  keyboard: true,
  ariaLabelledBy: 'command-palette-title',
};

@Injectable({
  providedIn: 'root',
})
export class CommandPaletteService {

  private readonly modalService = inject(ModalService);

  open(): void {
    if (this.modalService.hasOpenModals()) return;

    this.modalService.open(CommandPaletteComponent, COMMAND_PALETTE_MODAL_OPTIONS);
  }
}
