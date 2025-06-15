export abstract class IClearableRepository {
  abstract clear(): Promise<void>;
}
