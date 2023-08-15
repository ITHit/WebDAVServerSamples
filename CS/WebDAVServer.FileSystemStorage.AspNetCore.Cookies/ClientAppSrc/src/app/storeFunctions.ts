export enum ReducerType {
  pending = "pending",
  rejected = "rejected",
  fulfilled = "fulfilled",
}

export const getMidIgnoredPath = (storeName: string, storeField: string) => {
  return storeName + "." + storeField;
};

export const getMidIgnoredAction = (
  reducerTypePrefix: string,
  reducerType: ReducerType
) => {
  return reducerTypePrefix + "/" + reducerType;
};
