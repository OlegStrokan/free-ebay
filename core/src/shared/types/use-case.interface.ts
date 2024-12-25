export interface IUseCase<TRequest, TResponse> {
  execute(dto: TRequest): Promise<TResponse>;
}
