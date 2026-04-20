/**
 * Value object representing sort configuration
 */
export class SortOptions {
  constructor(
    public readonly column: string,
    public readonly ascending: boolean
  ) { }

  static default(): SortOptions {
    return new SortOptions('displayname', true);
  }

  toggleDirection(): SortOptions {
    return new SortOptions(this.column, !this.ascending);
  }

  withColumn(column: string): SortOptions {
    return new SortOptions(column, this.ascending);
  }
}
