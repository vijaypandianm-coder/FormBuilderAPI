using DbUp;
using DbUp.Helpers;
using Microsoft.Extensions.Configuration;

namespace FormBuilderAPI.Infrastructure.Db
{
    public static class MigrationRunner
    {
        public static void Run(IConfiguration cfg, ILogger logger)
        {
            var conn = cfg.GetConnectionString("Sql")
                      ?? throw new InvalidOperationException("Missing ConnectionStrings:Sql");

            var upgrader = DeployChanges.To
                .MySqlDatabase(conn)
                .WithScriptsFromFileSystem(Path.Combine(AppContext.BaseDirectory, "Sql", "Migrations"))
                .LogScriptOutput()
                .JournalTo(new NullJournal()) // we keep our own ledger table below
                .Build();

            var result = upgrader.PerformUpgrade();
            if (!result.Successful)
                throw result.Error;

            logger.LogInformation("SQL migrations ran successfully.");
        }
    }
}