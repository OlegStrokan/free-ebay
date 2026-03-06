namespace Domain.Entities.DeliveryInfo;

public class DeliveryInfo
{
    public required string Id { get; init; }
    public required string Street { get; set; }
    public required string City { get; set; } 
    public required string PostalCode { get; set; }
    public required string CountryDestination { get; set; }
}