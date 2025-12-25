import {computed, signal} from '@angular/core';

export class Tracker<T, S> {

  private readonly idFunc: (t: T) => S;
  private readonly comparator: (a: T, b: T) => boolean;

  private readonly _items = signal<T[]>([]);
  private readonly _lookup = signal(new Map<S, T>());

  public readonly items = this._items.asReadonly();
  public readonly ids = computed(() => this.items().map(x => this.idFunc(x)));
  public readonly lookup = this._lookup.asReadonly();
  public readonly empty = computed(() => this._items().length === 0);

  /**
   *
   * @param idFunc Return the identifier for the items
   * @param comparator If required, pass a custom comparator. Default to === between ids
   */
  constructor(idFunc: (t: T) => S, comparator?: (a: T, b: T) => boolean) {
    this.idFunc = idFunc;
    this.comparator = comparator ?? ((a: T, b: T) => this.idFunc(a) === this.idFunc(b));
  }

  add(item: T) {
    const id = this.idFunc(item);
    if (this._lookup().has(id)) return false;

    this._items.update(items => [...items, item]);
    this._lookup.update(map => new Map(map).set(id, item));

    return true;
  }

  addAll(items: T[]) {
    items.forEach(item => this.add(item));
  }

  remove(item: T) {
    const id = this.idFunc(item);
    if (!this._lookup().has(id)) return false;

    this._items.update(items => items.filter(t => !this.comparator(t, item)));

    this._lookup.update(map => {
      const newMap = new Map(map);
      newMap.delete(id);
      return newMap;
    });

    return true;
  }

  removeAll(items: T[]) {
    items.forEach(item => this.remove(item));
  }

  toggle(item: T) {
    const id = this.idFunc(item);
    if (this._lookup().has(id)) {
      return this.remove(item);
    }

    return this.add(item);
  }

  reset() {
    this._items.set([]);
    this._lookup.set(new Map<S, T>());
  }
}
