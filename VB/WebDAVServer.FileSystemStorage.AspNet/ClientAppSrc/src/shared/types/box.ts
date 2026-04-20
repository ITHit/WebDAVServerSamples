/** Framework-neutral readable box abstraction (compatible with Vue Ref<T>). */
export interface ReadonlyBox<T> {
  readonly value: T;
}

/** Framework-neutral mutable box abstraction (compatible with Vue Ref<T>). */
export interface MutableBox<T> extends ReadonlyBox<T> {
  value: T;
}
