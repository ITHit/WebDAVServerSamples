import { ValidationError } from '@/shared/types/appErrors';

const INVALID_NAME_CHARS = /[<>:"/\\|?*]/;

export function validateFolderName(folderName: string): string {
  const trimmedName = folderName.trim();
  if (!trimmedName) {
    throw new ValidationError('phrases.validations.folderNameCannotBeEmpty');
  }
  if (INVALID_NAME_CHARS.test(trimmedName)) {
    throw new ValidationError('phrases.validations.folderNameContainsInvalidCharacters');
  }
  return trimmedName;
}

export function validateRenameName(newName: string): string {
  const trimmedName = newName.trim();
  if (!trimmedName) {
    throw new ValidationError('phrases.validations.newNameCannotBeEmpty');
  }
  if (INVALID_NAME_CHARS.test(trimmedName)) {
    throw new ValidationError('phrases.validations.nameContainsInvalidCharacters');
  }
  return trimmedName;
}

export function validateMoveItems(itemPaths: string[], targetPath: string): void {
  if (!itemPaths.length) {
    throw new ValidationError('phrases.validations.noItemsSelectedForMoving');
  }

  const normalizedTarget = targetPath.replace(/\/+$/, '');

  for (const path of itemPaths) {
    const normalizedPath = path.replace(/\/+$/, '');

    if (normalizedPath === normalizedTarget) {
      throw new ValidationError('phrases.validations.cannotMoveItemToItself');
    }

    if (normalizedTarget.startsWith(normalizedPath + '/')) {
      throw new ValidationError('phrases.validations.cannotMoveParentToChild');
    }
  }
}

export function validateCopyItems(itemPaths: string[], targetPath: string): void {
  if (!itemPaths.length) {
    throw new ValidationError('phrases.validations.noItemsSelectedForCopying');
  }

  const normalizedTarget = targetPath.replace(/\/+$/, '');

  for (const path of itemPaths) {
    const normalizedPath = path.replace(/\/+$/, '');

    if (normalizedTarget.startsWith(normalizedPath + '/')) {
      throw new ValidationError('phrases.validations.cannotCopyParentToChild');
    }
  }
}
