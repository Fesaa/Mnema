import {ChangeDetectionStrategy, Component, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {JsonPipe} from '@angular/common';
import {ContentFormat} from '@mnema/features/monitored-series/monitored-series.service';
import {ParseResult} from '@mnema/_models/parse-result';
import {ParserService} from '@mnema/_services/parser.service';

@Component({
  selector: 'app-parser-tester',
  standalone: true,
  imports: [
    FormsModule,
    JsonPipe
  ],
  templateUrl: './parser-tester.component.html',
  styleUrl: './parser-tester.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ParserTesterComponent {

  private readonly parserService = inject(ParserService);

  protected readonly contentFormats: { name: string; value: ContentFormat }[] = (
    Object.keys(ContentFormat).filter(k => isNaN(Number(k))) as (keyof typeof ContentFormat)[]
  ).map(name => ({name, value: ContentFormat[name]}));

  protected query = '';
  protected selectedFormat: ContentFormat = ContentFormat.Manga;

  protected readonly result = signal<ParseResult | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(false);

  protected parse(): void {
    if (!this.query.trim()) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.result.set(null);

    this.parserService.testParse(this.query, this.selectedFormat).subscribe({
      next: res => {
        this.result.set(res);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err?.error?.message ?? err?.message ?? 'Parse request failed');
        this.loading.set(false);
      }
    });
  }
}
