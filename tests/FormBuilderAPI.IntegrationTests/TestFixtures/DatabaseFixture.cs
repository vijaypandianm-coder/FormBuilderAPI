using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using System.Threading.Tasks;
using Testcontainers.MongoDb;
using Testcontainers.MySql;

namespace FormBuilderAPI.IntegrationTests.TestFixtures
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private readonly MySqlContainer _mySqlContainer;
        private readonly MongoDbContainer _mongoContainer;

        public string MySqlConnectionString => _mySqlContainer.GetConnectionString();
        public string MongoConnectionString => _mongoContainer.GetConnectionString();

        public DatabaseFixture()
        {
            _mySqlContainer = new MySqlBuilder()
                .WithImage("mysql:8.0")
                .WithDatabase("formbuilder_test")
                .WithUsername("testuser")
                .WithPassword("testpassword")
                .WithPortBinding(3307, 3306)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(3306))
                .Build();

            _mongoContainer = new MongoDbBuilder()
                .WithImage("mongo:6.0")
                .WithPortBinding(27018, 27017)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(27017))
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _mySqlContainer.StartAsync();
            await _mongoContainer.StartAsync();

            // Run migrations or seed data if needed
        }

        public async Task DisposeAsync()
        {
            await _mySqlContainer.StopAsync();
            await _mongoContainer.StopAsync();
        }
    }
}