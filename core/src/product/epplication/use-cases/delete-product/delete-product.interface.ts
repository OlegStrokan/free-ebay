export abstract class IDeleteProductUseCase {
  abstract execute(productId: string): Promise<void>;
}
