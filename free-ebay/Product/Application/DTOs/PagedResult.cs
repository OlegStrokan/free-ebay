namespace Application.DTOs;

public sealed record PagedResult<T>(List<T> Items, int TotalCount, int Page, int Size);
