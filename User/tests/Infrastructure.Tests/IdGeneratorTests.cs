using Infrastructure.Helpers;

namespace Infrastructure.Tests;

public class IdGeneratorTests
{

    [Fact]
    public void ShouldReturnUniqueStrings()
    {
        var idGenerator = new IdGenerator();

        var id1 = idGenerator.GenerateId();
        var id2 = idGenerator.GenerateId();

        Assert.NotNull(id1);
        Assert.NotNull(id2);
        Assert.NotEqual(id1, id2);
    }
}