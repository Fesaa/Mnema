import {Component, EventEmitter, HostListener, inject, Input, OnInit, Output} from '@angular/core';
import {catchError, of, ReplaySubject, tap} from "rxjs";
import {DirEntry} from "../../../_models/io";
import {Stack} from "../../data-structures/stack";
import {IoService} from "../../../_services/io.service";
import {Clipboard} from "@angular/cdk/clipboard";
import {FormsModule} from "@angular/forms";
import {ToastService} from "../../../_services/toast.service";
import {TranslocoDirective} from "@jsverse/transloco";
import {TitleCasePipe} from "@angular/common";
import {NgbActiveModal} from "@ng-bootstrap/ng-bootstrap";

@Component({
  selector: 'app-directory-selector',
  imports: [
    FormsModule,
    TranslocoDirective,
    TitleCasePipe
  ],
  templateUrl: './directory-selector.component.html',
  styleUrl: './directory-selector.component.scss'
})
export class DirectorySelectorComponent implements OnInit {

  private readonly modal = inject(NgbActiveModal);

  @Input() isMobile = false;

  @Input({required: true}) root!: string;
  @Input() showFiles: boolean = false;
  @Input() filter: boolean = false;
  @Input() copy: boolean = true;
  @Input() create: boolean = false;
  @Input() customWidth: string = '50vw';

  @Input() visible: boolean = true;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();

  @Output() resultDir = new EventEmitter<string | undefined>();

  currentRoot = '';
  entries: DirEntry[] = [];
  routeStack: Stack<string> = new Stack<string>();

  query: string = '';
  newDirName: string = '';
  private result = new ReplaySubject<string | undefined>(1)

  constructor(private ioService: IoService,
              private toastService: ToastService,
              private clipboard: Clipboard,
  ) {
  }

  getEntries() {
    return this.entries.filter(entry => this.normalize(entry.name).includes(this.query));
  }

  selectNode(entry: DirEntry) {
    if (!entry.dir) {
      return;
    }

    this.currentRoot = entry.name;
    this.routeStack.push(entry.name);
    this.loadChildren(this.routeStack.items.join('/')).pipe(
      tap(() => this.query = ''),
    ).subscribe();
  }

  goBack() {
    if (this.routeStack.isEmpty()) {
      return;
    }

    this.routeStack.pop();
    const nextRoot = this.routeStack.peek();
    if (nextRoot) {
      this.currentRoot = nextRoot;
    }
    this.loadChildren(this.routeStack.items.join('/')).subscribe();
  }

  normalize(str: string): string {
    return str.toLowerCase();
  }

  onFilterChange(event: Event) {
    const inputElement = event.target as HTMLInputElement;
    this.query = this.normalize(inputElement.value);
  }

  onNewDirNameChange(event: Event) {
    const inputElement = event.target as HTMLInputElement;
    this.newDirName = inputElement.value;
  }

  createNew() {
    this.ioService.create(this.routeStack.items.join('/'), this.newDirName).subscribe({
      next: () => {
        this.toastService.successLoco("directory-selector.toasts.create.success", {}, {name: this.newDirName})
        this.newDirName = '';
        this.loadChildren(this.routeStack.items.join('/')).subscribe();
      },
      error: (err) => {
        this.toastService.errorLoco("directory-selector.toasts.create.error", {}, {msg: err.error.message})
      }
    });
  }

  copyPath(entry: DirEntry) {
    let path = this.routeStack.items.join('/');
    if (entry.dir) {
      path += '/' + entry.name;
    }
    this.clipboard.copy(path);
  }

  @HostListener('window:resize', [])
  onResize() {
    this.isMobile = window.innerWidth < 768;
  }

  public getResult() {
    return this.result.asObservable();
  }

  closeModal() {
    this.result.next(undefined);
    this.resultDir.emit(undefined);
    this.result.complete();
    this.visibleChange.emit(false);
    this.modal.close();
  }

  confirm() {
    let path = this.routeStack.items.join('/');
    if (path.startsWith('/')) {
      path = path.substring(1);
    }
    this.result.next(path);
    this.resultDir.emit(path);
    this.result.complete();
    this.visibleChange.emit(false);
    this.modal.close();
  }

  private loadChildren(dir: string) {
    return this.ioService.ls(dir, this.showFiles).pipe(
      tap(entries => {
        this.entries = entries || [];
      }),
      catchError(err => {
        this.routeStack.pop();
        this.toastService.genericError(err.error.message);
        return of(null);
      })
    )
  }

  ngOnInit(): void {
    this.currentRoot = this.root;
    this.routeStack.push(this.root);
    this.loadChildren(this.root).subscribe();
    this.isMobile = window.innerWidth < 768;
  }

}
