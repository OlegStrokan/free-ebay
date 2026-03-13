namespace Application.Models;

// Full denormalized Elasticsearch document for the "products" index.
// Created on ProductCreatedEvent; fully replaced on subsequent writes.
public sealed class ProductSearchDocument
{
    public required string Id { get; set; }  // ProductId UUID — used as ES document _id
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string CategoryId { get; set; }  // UUID string reference
    public required decimal Price { get; set; }
    public required string Currency { get; set; }
    public required int Stock { get; set; }
    public required Dictionary<string, string> Attributes { get; set; }  // e.g. {"color":"red","layout":"tenkeyless"}
    public required List<string> ImageUrls { get; set; }
    public required string Status { get; set; }  // "Draft" | "Active" | "Inactive" | "OutOfStock" | "Deleted"
    public required string SellerId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
