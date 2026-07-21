using TaskFlow.Infrastructure.Auth;

namespace TaskFlow.UnitTests.Auth;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Acme Inc", "acme-inc")]
    [InlineData("  Padded Name  ", "padded-name")]
    [InlineData("ALL CAPS", "all-caps")]
    [InlineData("Dots.and,Commas!", "dots-and-commas")]
    [InlineData("Multiple   Spaces", "multiple-spaces")]
    [InlineData("--Leading-And-Trailing--", "leading-and-trailing")]
    public void Generate_ProducesExpectedSlug(string name, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Generate(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData("---")]
    public void Generate_WithNoAlphanumericContent_FallsBackToOrg(string name)
    {
        Assert.Equal("org", SlugGenerator.Generate(name));
    }
}
