export abstract class IDeleteUserUseCase {
  abstract execute(userId: string): Promise<void>;
}
