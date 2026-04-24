namespace Application.Models;

public sealed class ProductSearchDocument
{
    public required string Id { get; set; }  
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string CategoryId { get; set; }
    public required decimal Price { get; set; }
    public required string Currency { get; set; }
    public required int Stock { get; set; }
    public required Dictionary<string, string> Attributes { get; set; }  //  {"color":"red","layout":"tenkeyless"}
    public required List<string> ImageUrls { get; set; }
    public required string Status { get; set; }  // "Draft" | "Active" | "Inactive" | "OutOfStock" | "Deleted"
    public required string SellerId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
