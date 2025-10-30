using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace FormBuilderAPI.IntegrationTests.TestFixtures
{
    public class ApiWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _mySqlConnectionString;
        private readonly string _mongoConnectionString;

        public ApiWebApplicationFactory(string mySqlConnectionString, string mongoConnectionString)
        {
            _mySqlConnectionString = mySqlConnectionString;
            _mongoConnectionString = mongoConnectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                var configDictionary = new Dictionary<string, string>
                {
                    {"ConnectionStrings:Sql", _mySqlConnectionString},
                    {"MongoDb:ConnectionString", _mongoConnectionString},
                    {"MongoDb:DatabaseName", "FormBuilderTest"},
                    {"Jwt:Issuer", "test-issuer"},
                    {"Jwt:Audience", "test-audience"},
                    {"Jwt:Key", "super-secret-test-key-with-at-least-32-characters"},
                    {"Jwt:TokenValidityInMinutes", "60"}
                };

                config.AddInMemoryCollection(configDictionary);
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test", options => { });
            });

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });
        }
    }
}