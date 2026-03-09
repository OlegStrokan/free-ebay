namespace Infrastructure.Persistence.ReadModels;

public sealed class Category
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
}
