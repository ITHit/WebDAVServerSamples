import { ValidationError } from '@/shared/types/appErrors';

/**
 * Value object representing pagination configuration
 */
export class PaginationOptions {
  constructor(
    public readonly page: number,
    public readonly pageSize: number
  ) {
    if (page < 1) throw new ValidationError('phrases.validations.pageMustBePositive');
    if (pageSize < 1) throw new ValidationError('phrases.validations.pageSizeMustBePositive');
  }

  static default(): PaginationOptions {
    return new PaginationOptions(1, 20);
  }

  get offset(): number {
    return (this.page - 1) * this.pageSize;
  }

  nextPage(): PaginationOptions {
    return new PaginationOptions(this.page + 1, this.pageSize);
  }

  previousPage(): PaginationOptions {
    if (this.page <= 1) return this;
    return new PaginationOptions(this.page - 1, this.pageSize);
  }

  withPageSize(pageSize: number): PaginationOptions {
    return new PaginationOptions(1, pageSize);
  }

  calculateTotalPages(totalItems: number): number {
    return Math.ceil(totalItems / this.pageSize);
  }
}
