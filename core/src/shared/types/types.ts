// eslint-disable-next-line @typescript-eslint/ban-types
type ImmutablePrimitive =
  | undefined
  | null
  | boolean
  | string
  | number
  | Function;

export type Immutable<T> = T extends ImmutablePrimitive
  ? T
  : T extends Map<infer K, infer V>
  ? ImmutableMap<K, V>
  : T extends Set<infer M>
  ? ImmutableSet<M>
  : ImmutableObject<T>;

export type ImmutableArray<T> = ReadonlyArray<Immutable<T>>;
export type ImmutableMap<K, V> = ReadonlyMap<Immutable<K>, Immutable<V>>;
export type ImmutableSet<T> = ReadonlySet<Immutable<T>>;
export type ImmutableObject<T> = { readonly [K in keyof T]: Immutable<T[K]> };

export type DeepPartial<T> = T extends object
  ? {
      [K in keyof T]?: DeepPartial<T[K]>;
    }
  : T;

export type Optional<TObject, TKeys extends keyof TObject> = Omit<
  TObject,
  TKeys
> &
  Partial<TObject>;

export type Primitive = number | bigint | string | boolean | symbol;

export type Key = number | string | symbol;

export type AnyRecord = Record<Key, unknown>;

export type StripPrefix<
  Prefix extends string,
  TString extends string,
> = TString extends `${Prefix}${infer WithoutPrefix}` ? WithoutPrefix : TString;

/**
 * Prefixes keys of an object
 */
export type PrefixKeys<
  T extends Record<string, unknown>,
  Prefix extends string,
> = {
  [PrefixedKey in `${Prefix}${keyof T &
    string}`]: PrefixedKey extends `${Prefix}${infer Key}` ? T[Key] : never;
};

/**
 * Use this primarily to specify the return type of functions that may or may not return a Promise.
 */
export type Awaitable<T> = T | Promise<T>;

export type OptionalKeys<T, TKeys extends keyof T> = Partial<Pick<T, TKeys>> &
  Omit<T, TKeys>;

export type Ulid = string;
