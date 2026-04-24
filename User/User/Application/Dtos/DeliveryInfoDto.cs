using System.Collections.Generic;
using System.Linq;
using DeliveryInfoEntity = Domain.Entities.DeliveryInfo.DeliveryInfo;

namespace Application.Dtos;

public record DeliveryInfoDto(
    string Id,
    string Street,
    string City,
    string PostalCode,
    string CountryDestination);

public static class DeliveryInfoMappingExtensions
{
    public static DeliveryInfoDto ToDto(this DeliveryInfoEntity entity)
        => new(entity.Id, entity.Street, entity.City, entity.PostalCode, entity.CountryDestination);

    public static List<DeliveryInfoDto> ToDtos(this IEnumerable<DeliveryInfoEntity> entities)
        => entities.Select(e => e.ToDto()).ToList();
}
