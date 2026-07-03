using ConfigLens.Application.Analysis;
using Shouldly;
using Xunit;

namespace ConfigLens.Application.Tests.Analysis;

public class ShannonEntropyTests
{
    [Theory]
    [InlineData("", 0.0)]
    [InlineData("a", 0.0)]
    [InlineData("aaaa", 0.0)]
    [InlineData("abab", 1.0)]
    [InlineData("abcd", 2.0)]
    [InlineData("abcdefgh", 3.0)]
    public void Known_distributions_yield_exact_entropy(string value, double expected)
    {
        ShannonEntropy.OfString(value).ShouldBe(expected, tolerance: 1e-9);
    }

    [Fact]
    public void Random_looking_token_has_higher_entropy_than_natural_language()
    {
        var natural = ShannonEntropy.OfString("connectionstring");
        var random = ShannonEntropy.OfString("Zq7xW3vN9pK2mR8tY5uB1cD6eF4gH0sL");

        random.ShouldBeGreaterThan(natural);
        random.ShouldBeGreaterThanOrEqualTo(4.0);
    }

    [Fact]
    public void Entropy_is_independent_of_character_order()
    {
        ShannonEntropy.OfString("abcabc").ShouldBe(ShannonEntropy.OfString("aabbcc"), tolerance: 1e-9);
    }

    [Fact]
    public void Null_is_rejected()
    {
        Should.Throw<ArgumentNullException>(() => ShannonEntropy.OfString(null!));
    }
}
