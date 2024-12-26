export class InvalidProductStatusError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidProductStatusError';
  }
}

export class ProductNotFoundError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'ProductNotFoundError';
  }
}
