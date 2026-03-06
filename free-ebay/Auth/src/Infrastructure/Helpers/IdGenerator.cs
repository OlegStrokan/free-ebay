namespace Infrastructure.Helpers;

public class IdGenerator
{
    public string GenerateId()
    {
        return Ulid.NewUlid().ToString();
    }
}