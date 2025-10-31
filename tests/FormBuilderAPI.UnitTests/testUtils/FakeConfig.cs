using Microsoft.Extensions.Configuration;

namespace FormBuilderAPI.UnitTests.TestUtils;

public static class FakeConfig
{
    public static IConfiguration Build(IDictionary<string,string?>? overrides = null)
    {
        var dict = new Dictionary<string,string?>
        {
            ["Jwt:Key"] = "super-secret-key-for-tests-1234567890",
            ["Jwt:Issuer"] = "FormBuilder",
            ["Jwt:Audience"] = "FormBuilderUsers"
        };
        if (overrides != null)
            foreach (var kv in overrides) dict[kv.Key] = kv.Value;

        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }
}