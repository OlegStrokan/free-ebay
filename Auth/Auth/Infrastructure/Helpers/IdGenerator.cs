using Domain.Common.Interfaces;

namespace Infrastructure.Helpers;

public class IdGenerator : IIdGenerator
{
    public string GenerateId()
    {
        return Ulid.NewUlid().ToString();
    }
}