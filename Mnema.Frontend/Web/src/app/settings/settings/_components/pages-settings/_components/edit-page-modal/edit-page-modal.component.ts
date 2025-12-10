import {ChangeDetectionStrategy, Component, inject, model, OnInit, signal} from '@angular/core';
import {AllProviders, Modifier, ModifierType, Page, Provider} from "../../../../../../_models/page";
import {
  NgbActiveModal,
  NgbNav,
  NgbNavContent,
  NgbNavItem,
  NgbNavItemRole,
  NgbNavLink,
  NgbNavOutlet,
} from "@ng-bootstrap/ng-bootstrap";
import {PageService} from "../../../../../../_services/page.service";
import {translate, TranslocoDirective} from "@jsverse/transloco";
import {AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators} from "@angular/forms";
import {SafeHtmlPipe} from "../../../../../../_pipes/safe-html-pipe";
import {SettingsItemComponent} from "../../../../../../shared/form/settings-item/settings-item.component";
import {DefaultValuePipe} from "../../../../../../_pipes/default-value.pipe";
import {ModalService} from "../../../../../../_services/modal.service";
import {BadgeComponent} from "../../../../../../shared/_component/badge/badge.component";
import {TypeaheadComponent, TypeaheadSettings} from "../../../../../../type-ahead/typeahead.component";
import {ProviderNamePipe} from "../../../../../../_pipes/provider-name.pipe";
import {of} from "rxjs";
import {CdkDragDrop, CdkDragHandle, moveItemInArray} from "@angular/cdk/drag-drop";
import {TableComponent} from "../../../../../../shared/_component/table/table.component";
import {EditPageModifierModalComponent} from "../edit-page-modifier-modal/edit-page-modifier-modal.component";
import {DefaultModalOptions} from "../../../../../../_models/default-modal-options";
import {ToastService} from "../../../../../../_services/toast.service";

@Component({
  selector: 'app-edit-page-modal',
  imports: [
    TranslocoDirective,
    ReactiveFormsModule,
    NgbNav,
    NgbNavItemRole,
    NgbNavItem,
    NgbNavLink,
    NgbNavContent,
    NgbNavOutlet,
    SafeHtmlPipe,
    SettingsItemComponent,
    DefaultValuePipe,
    BadgeComponent,
    TypeaheadComponent,
    ProviderNamePipe,
    CdkDragHandle,
    TableComponent
  ],
  templateUrl: './edit-page-modal.component.html',
  styleUrl: './edit-page-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditPageModalComponent implements OnInit {

  private readonly modalService = inject(ModalService);
  private readonly modal = inject(NgbActiveModal);
  private readonly pageService = inject(PageService);
  private readonly providerNamePipe = inject(ProviderNamePipe);
  private readonly toastService = inject(ToastService);

  page = model.required<Page>();

  pageForm = new FormGroup({});
  selectedProviders = signal<Provider[]>([]);

  activeTab = 'general';

  ngOnInit(): void {
    const page = this.page();

    this.selectedProviders.set(page.providers);
    this.pageForm.addControl('title', new FormControl(page.title, [Validators.required]));
    this.pageForm.addControl('icon', new FormControl(page.icon, []));
    this.pageForm.addControl('customRootDir', new FormControl(page.customRootDir, []));
    this.pageForm.addControl('providers', new FormControl(page.providers, []));
    this.pageForm.addControl('dirs', new FormControl(page.dirs.join(','), []));
    this.pageForm.addControl('modifiers', new FormArray(page.modifiers.map(m => this.modifierFormGroup(m))))
  }

  private modifierFormGroup(m: Modifier) {
    return new FormGroup({
      title: new FormControl(m.title, [Validators.required]),
      key: new FormControl(m.key, [Validators.required]),
      type: new FormControl(m.type, [Validators.required]),
      values: new FormArray(m.values.map(mv => {
        return new FormGroup({
          key: new FormControl(mv.key, [Validators.required]),
          value: new FormControl(mv.value, [Validators.required]),
          default: new FormControl(mv.default, []),
        });
      }))
    })
  }

  get modifiersFormArray(): FormArray {
    return this.pageForm.get('modifiers') as unknown as FormArray;
  }

  async pickCustomRootDir() {
    const dir = await this.modalService.getDirectory('', {showFiles: false, create: true});
    if (dir) {
      (this.pageForm.get('customRootDir') as unknown as FormControl<string>)?.setValue(dir);
    }
  }

  async addDir() {
    const dir = await this.modalService.getDirectory('', {showFiles: false, create: true});
    const dirs = this.break(this.pageForm.get('dirs')?.value as any || '');

    if (!dir || dirs.includes(dir)) return;

    dirs.push(dir);
    (this.pageForm.get('dirs') as unknown as FormControl<string>)?.setValue(dirs.join(','));
  }

  providerTypeaheadSettings(): TypeaheadSettings<Provider> {
    const settings = new TypeaheadSettings<Provider>();
    settings.id = "page-providers"
    settings.minCharacters = 0;
    settings.multiple = true;

    settings.fetchFn = (f) =>
      of(AllProviders.filter(p =>
        this.providerNamePipe.transform(p).toLowerCase().includes(f.toLowerCase())));
    settings.savedData = this.selectedProviders();

    return settings;
  }

  updateSelectedProviders(event: Provider | Provider[]) {
    this.selectedProviders.set(event as Provider[]);
  }

  break(s: string) {
    if (s) return s.split(',').filter(s => s.trim() !== '');

    return [];
  }

  close() {
    this.modal.close();
  }

  save() {
    if (!this.pageForm.valid) return;

    const page = this.pageForm.value as any;
    page.ID = this.page().id;
    page.dirs = this.break(page.dirs);
    page.modifiers.forEach((m: Modifier) => {
      m.type = parseInt(m.type+'');
    });
    page.sortValue = this.page().sortValue;
    page.providers = this.selectedProviders();


    const action$ = this.page().id === 0
      ? this.pageService.new(page)
      : this.pageService.update(page);

    action$.subscribe({
      next: (page) => {
        this.toastService.successLoco("settings.pages.toasts.save.success");
        this.pageService.refreshPages().subscribe();
      },
      error: (error) => {
        this.toastService.errorLoco("settings.pages.toasts.save.error", {}, {msg: error.error.message});
      }
    }).add(() => this.close())
  }

  editModifier(modifier: FormGroup) {
    const [modal, component] = this.modalService.open(EditPageModifierModalComponent, DefaultModalOptions);
    component.modifierForm.set(modifier);
  }

  async deleteModifier(title: string, idx: number) {
    if (title) {
      if (!await this.modalService.confirm({
        question: translate('edit-page-modal.delete-modifier', {title: title})
      })) {
        return;
      }
    }

    this.modifiersFormArray.removeAt(idx);
  }

  addModifier() {
    this.modifiersFormArray.push(this.modifierFormGroup({
      title: '',
      key: '',
      type: ModifierType.DROPDOWN,
      values: [],
    }));
  }

  sortModifiers($event: CdkDragDrop<AbstractControl[], any>) {
    const controls = this.modifiersFormArray.controls;
    moveItemInArray(controls, $event.previousIndex, $event.currentIndex)
    this.modifiersFormArray.patchValue(controls);
  }

  modifierTrack(idx: number, m: AbstractControl) {
    return `${m.get('key')}`;
  }
}
