using ConfigLens.Application.Analysis;
using Shouldly;
using Xunit;

namespace ConfigLens.Application.Tests.Analysis;

public class LevenshteinTests
{
    [Theory]
    [InlineData("", "", 0)]
    [InlineData("abc", "abc", 0)]
    [InlineData("abc", "ABC", 0)]
    [InlineData("abc", "", 3)]
    [InlineData("", "abc", 3)]
    [InlineData("App:Timeout", "App:Timeuot", 2)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("Smtp:Host", "Smtp:Hots", 2)]
    [InlineData("Retries", "Retry", 3)]
    public void Distance_matches_known_values(string left, string right, int expected)
    {
        Levenshtein.Distance(left, right).ShouldBe(expected);
    }

    [Fact]
    public void Distance_is_symmetric()
    {
        Levenshtein.Distance("Timeout", "Timeuot").ShouldBe(Levenshtein.Distance("Timeuot", "Timeout"));
    }
}
