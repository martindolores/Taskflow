using TaskFlow.Infrastructure.Auth;

namespace TaskFlow.UnitTests.Auth;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesValueDifferentFromPlaintext()
    {
        var hash = _hasher.Hash("correct-horse-battery");

        Assert.NotEqual("correct-horse-battery", hash);
    }

    [Fact]
    public void Verify_WithMatchingPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("correct-horse-battery");

        Assert.True(_hasher.Verify("correct-horse-battery", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("correct-horse-battery");

        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_CalledTwiceWithSamePassword_ProducesDifferentHashes()
    {
        var first = _hasher.Hash("correct-horse-battery");
        var second = _hasher.Hash("correct-horse-battery");

        Assert.NotEqual(first, second);
    }
}
