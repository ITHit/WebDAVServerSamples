const pathsMap = {
  home: () => "*",
};
type PathsMap = typeof pathsMap;

export const getPath = <TRoute extends keyof PathsMap>(
  route: TRoute,
  ...params: Parameters<PathsMap[TRoute]>
) => {
  const pathCb: (...args: unknown[]) => string = pathsMap[route];
  return pathCb(...params);
};
