using ConfigLens.Application;
using ConfigLens.Application.Rules;
using ConfigLens.Domain;
using Shouldly;
using Xunit;

namespace ConfigLens.Application.Tests.Rules;

public class HardcodedSecretRuleTests
{
    private readonly HardcodedSecretRule _rule = new();

    private static RuleContext Context(string key, string? value)
    {
        var entry = new ConfigEntry(
            ConfigKey.Parse(key),
            value,
            ConfigModel.BaseEnvironment,
            new SourceLocation("appsettings.json", 7));
        return new RuleContext(new ConfigModel([entry]), UsageModel.Empty, new ScanRequest("."));
    }

    private Finding SingleFinding(string key, string value)
        => _rule.Evaluate(Context(key, value)).ShouldHaveSingleItem();

    private void AssertClean(string key, string? value)
        => _rule.Evaluate(Context(key, value)).ShouldBeEmpty();

    [Fact]
    public void Connection_string_with_inline_password_is_high_confidence()
    {
        var finding = SingleFinding("ConnectionStrings:Default", "Server=db;User Id=sa;Password=SuperSecret123!;");

        finding.RuleId.ShouldBe("CL004");
        finding.Severity.ShouldBe(Severity.Error);
        finding.Confidence.ShouldBe(Confidence.High);
    }

    [Theory]
    [InlineData("AKIAIOSFODNN7EXAMPLE")]
    [InlineData("ghp_1234567890abcdefghijklmnopqrstuvwxyz")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U")]
    public void Well_known_token_formats_are_high_confidence(string value)
    {
        SingleFinding("Some:Value", value).Confidence.ShouldBe(Confidence.High);
    }

    [Fact]
    public void Private_key_block_is_high_confidence()
    {
        SingleFinding("Tls:Key", "-----BEGIN RSA PRIVATE KEY-----\\nMIIB...").Confidence.ShouldBe(Confidence.High);
    }

    [Fact]
    public void Secret_key_name_with_plain_value_is_medium_confidence()
    {
        var finding = SingleFinding("Auth:ClientSecret", "hunter2-prod");

        finding.Confidence.ShouldBe(Confidence.Medium);
        finding.Message.ShouldContain("ClientSecret");
    }

    [Fact]
    public void Secret_key_name_with_high_entropy_value_is_high_confidence()
    {
        SingleFinding("Auth:ApiKey", "Zq7xW3vN9pK2mR8tY5uB1cD6eF4gH0sLaJdOiUwE")
            .Confidence.ShouldBe(Confidence.High);
    }

    [Fact]
    public void High_entropy_value_without_secret_name_is_low_confidence()
    {
        SingleFinding("Signing:Material", "R8f2Kd91LmQz74XcVb06NwEy35TgHaJp")
            .Confidence.ShouldBe(Confidence.Low);
    }

    [Fact]
    public void Messages_redact_the_secret_value()
    {
        var finding = SingleFinding("Auth:ApiKey", "Zq7xW3vN9pK2mR8tY5uB1cD6eF4gH0sLaJdOiUwE");

        finding.Message.ShouldNotContain("Zq7xW3vN9pK2mR8tY5uB1cD6eF4gH0sLaJdOiUwE");
        finding.Message.ShouldContain("Zq7x…");
    }

    [Theory]
    [InlineData("Auth:ApiKey", "${API_KEY}")]
    [InlineData("Auth:ApiKey", "<your-api-key-here>")]
    [InlineData("Auth:ApiKey", "{{vault:api-key}}")]
    [InlineData("Auth:ApiKey", "%API_KEY%")]
    [InlineData("Auth:ApiKey", "changeme")]
    [InlineData("Auth:Password", "placeholder")]
    public void Placeholders_are_not_secrets(string key, string value)
    {
        AssertClean(key, value);
    }

    [Theory]
    [InlineData("Auth:TokenLifetimeMinutes", "60")]
    [InlineData("Auth:RequireHttpsMetadata", "true")]
    [InlineData("App:Name", "OrdinaryValue")]
    [InlineData("Api:BaseUrl", "https://api.example.com")]
    [InlineData("Empty:Value", "")]
    [InlineData("Null:Value", null)]
    public void Ordinary_configuration_values_are_not_secrets(string key, string? value)
    {
        AssertClean(key, value);
    }

    [Fact]
    public void Guids_are_not_flagged_by_the_entropy_heuristic()
    {
        AssertClean("Tenant:Id", "d3b07384-d9a0-4c9e-8b7a-1f2e3d4c5b6a");
    }

    [Fact]
    public void Connection_string_without_password_is_clean()
    {
        AssertClean("ConnectionStrings:Default", "Server=db;Database=app;Integrated Security=true;");
    }
}
