namespace Domain.ValueObjects;

public sealed record Address : ValueObject
{
    public string Street { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string Country { get; init; }
    public string PostalCode { get; init; }
    
    
    // explicit constructor for validation rules
    public Address(string street, string city, string state, string country, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(street)) 
            throw new ArgumentException("Street cannot be empty", nameof(street));
        
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));
        
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be empty", nameof(country));
        
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code cannot be empty", nameof(postalCode));
        
        Street = street;
        City = city;
        State = state ?? string.Empty;
        Country = country;
        PostalCode = postalCode;
    }
    
    public static Address Create(string street, string city, string state, string country, string postalCode) 
    => new Address(street, city, state, country, postalCode);

    public override string ToString() => $"{Street} {City} {State} {Country} {PostalCode}";

} 
