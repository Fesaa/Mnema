export interface NumberRange {
  value: string;
  minNumber: number;
  maxNumber: number;
}

export interface ParseResult {
  input: string;
  series: string[];
  volume: NumberRange;
  chapter: NumberRange;
}
