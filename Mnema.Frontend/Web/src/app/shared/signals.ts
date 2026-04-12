import {ActivatedRoute, Router} from "@angular/router";
import {effect, signal, WritableSignal} from "@angular/core";

type Primitive = string | number | boolean | null | undefined;
type QueryValue = Primitive | Date | Primitive[] | Date[];

function isDate(value: unknown): value is Date {
  return typeof value === 'object';
}

function serialize(value: QueryValue): any {
  if (value === null || value === undefined || value === '' || value === Infinity || Number.isNaN(value)) {
    return null;
  }

  if (Array.isArray(value)) {
    return value.map(v => (isDate(v) ? v.getTime() : v));
  }

  if (isDate(value)) {
    return value.getTime();
  }

  return value;
}

function deserialize(value: any, defaultValue: QueryValue): QueryValue {
  if (value === undefined) return defaultValue;

  if (Array.isArray(defaultValue)) {
    const arr = Array.isArray(value) ? value : [value];

    if (defaultValue.length && isDate(defaultValue[0])) {
      return arr.map(v => new Date(Number(v)));
    }

    return arr;
  }

  if (isDate(value)) {
    return new Date(Number(value));
  }

  if (typeof defaultValue === 'number') {
    return Number(value);
  }

  if (typeof defaultValue === 'boolean') {
    return value === 'true' || value === true;
  }

  return value;
}

export function querySignal<T extends QueryValue>(
  name: string,
  defaultValue: T,
  route: ActivatedRoute,
  router: Router
): WritableSignal<T>;

export function querySignal<T extends Record<string, QueryValue>>(
  defaultValue: T,
  route: ActivatedRoute,
  router: Router
): WritableSignal<T>;

export function querySignal<T>(
  nameOrDefault: string | T,
  defaultOrRoute: T | ActivatedRoute,
  routeOrRouter?: ActivatedRoute | Router,
  maybeRouter?: Router
): WritableSignal<T> {

  let name: string | undefined;
  let defaultValue: T;
  let route: ActivatedRoute;
  let router: Router;

  if (typeof nameOrDefault === 'string') {
    name = nameOrDefault;
    defaultValue = defaultOrRoute as T;
    route = routeOrRouter as ActivatedRoute;
    router = maybeRouter!;
  } else {
    defaultValue = nameOrDefault;
    route = defaultOrRoute as ActivatedRoute;
    router = routeOrRouter as Router;
  }

  const params = route.snapshot.queryParams;

  let initialValue: any;

  if (name) {
    const raw = params[name];
    initialValue = deserialize(raw, defaultValue as any);
  } else {
    initialValue = { ...defaultValue };

    for (const key of Object.keys(defaultValue as object)) {
      const raw = params[key];
      if (raw !== undefined) {
        initialValue[key] = deserialize(raw, (defaultValue as any)[key]);
      }
    }
  }

  const _signal = signal(initialValue);

  effect(() => {
    const value = _signal();
    const queryParams: any = {};

    if (name) {
      queryParams[name] = serialize(value as any);
    } else {
      for (const [key, val] of Object.entries(value as any)) {
        queryParams[key] = serialize(val as QueryValue);
      }
    }

    router.navigate([], {
      relativeTo: route,
      queryParams,
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  });

  return _signal as WritableSignal<T>;
}
