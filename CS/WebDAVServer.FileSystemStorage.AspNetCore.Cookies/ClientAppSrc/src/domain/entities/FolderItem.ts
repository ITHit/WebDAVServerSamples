/**
 * Domain entity representing a folder in the file system
 * Framework-agnostic representation
 */
export class FolderItem {
  constructor(
    public readonly id: string,
    public readonly name: string,
    public readonly path: string,
    public readonly modifiedAt: Date,
    public readonly locks: string[] = [],
  ) { }

  get isRoot(): boolean {
    return this.path === '/' || this.path === '';
  }

  get hasLocks(): boolean {
    return this.locks.length > 0;
  }
}
