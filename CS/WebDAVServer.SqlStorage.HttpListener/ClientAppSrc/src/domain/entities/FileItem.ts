/**
 * Domain entity representing a file in the file system
 * Framework-agnostic representation
 */
export class FileItem {
  constructor(
    public readonly id: string,
    public readonly name: string,
    public readonly path: string,
    public readonly size: number,
    public readonly modifiedAt: Date,
    public readonly contentType: string,
    public readonly locks: string[] = []
  ) { }

  get extension(): string {
    const lastDot = this.name.lastIndexOf('.');
    return lastDot > 0 ? this.name.substring(lastDot + 1) : '';
  }

  get hasLocks(): boolean {
    return this.locks.length > 0;
  }
}
