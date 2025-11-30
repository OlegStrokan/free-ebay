export type PaginatedResult<T, K extends string = 'items'> = {
  [P in K]: T[];
} & {
  nextCursor?: string;
};
