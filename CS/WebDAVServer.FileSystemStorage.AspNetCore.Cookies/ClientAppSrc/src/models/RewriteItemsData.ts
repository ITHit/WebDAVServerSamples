export class RewriteItemsData {
  onOverwrite: () => void;
  onSkipExists: () => void;
  itemsList: string;
  constructor(
    onOverwrite: () => void,
    onSkipExists: () => void,
    itemsList: string
  ) {
    this.onOverwrite = onOverwrite;
    this.onSkipExists = onSkipExists;
    this.itemsList = itemsList;
  }
}
