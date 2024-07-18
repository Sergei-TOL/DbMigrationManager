using DbUp;
using DbUp.Engine;
using DbUp.ScriptProviders;
using Microsoft.Extensions.Logging;

namespace DbMigrations;

public interface IDbMigrationsService
{
    Task EnsureDatabaseMigration(string connectionString, string migrationsScriptsPath, string idempotentScriptsPath);
}

public class DbMigrationsService(
    ILogger<DbMigrationsService> logger,
    IIdempotentScriptService idempotentScriptService
    ) : IDbMigrationsService
{
    private readonly ILogger _logger = logger;

    public async Task EnsureDatabaseMigration(string connectionString, string migrationsScriptsPath, string idempotentScriptsPath)
    {
        _logger.LogInformation($"Using migrations scripts from: {migrationsScriptsPath}");
        _logger.LogInformation($"Using idempotent scripts from: {idempotentScriptsPath}");

        ApplyDbUpMigrations(connectionString, migrationsScriptsPath);

        await idempotentScriptService.ApplyIdempotentScripts(connectionString, idempotentScriptsPath);
    }

    private void ApplyDbUpMigrations(string connectionString, string scriptsPath)
    {
        _logger.LogInformation("Apllying DbUp migrations from {scriptsPath}", scriptsPath);

        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var options = new FileSystemScriptOptions
        {
            IncludeSubDirectories = true,
            Encoding = DbUpDefaults.DefaultEncoding,
            Extensions = ["*.sql"],
            UseOnlyFilenameForScriptName = true
        };

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(new CustomPostgresqlConnectionManager(connectionString))
            .WithScriptsFromFileSystem(scriptsPath, options)
            .WithVariablesDisabled()
            .WithExecutionTimeout(TimeSpan.FromMinutes(10))
            .JournalToPostgresqlTable("public", "__dbup_journal")
            ;

        _logger.LogInformation("Package built");

        upgrader.Configure(x => { });
        _logger.LogInformation("Package reconfigured");

        var build = upgrader.Build();
        _logger.LogInformation("Package inialized");

        var connected = build.TryConnect(out string error);
        _logger.LogInformation("Connection established - {error}", error);

        var discoveredScripts = build.GetDiscoveredScripts();
        _logger.LogInformation("Scripts discovered ({count}): \n{scriptList}", discoveredScripts.Count, string.Join("\n", discoveredScripts.Select(x => x.Name)));

        var scriptsToExecute = build.GetScriptsToExecute();
        _logger.LogInformation("Scripts to execute ({count}): \n{scriptList}", scriptsToExecute.Count, string.Join("\n", scriptsToExecute.Select(x => x.Name)));

        var upgradeRequired = build.IsUpgradeRequired();
        _logger.LogInformation("Is upgrade required: {upgradeRequired}", upgradeRequired);

        if (!upgradeRequired)
            return;

        build.ScriptExecuted += (sender, eventArgs) =>
        {
            _logger.LogInformation("Script executed {scriptName}", (eventArgs as ScriptExecutedEventArgs)?.Script.Name);
        };

        _logger.LogInformation("Starting upgrade");
        var result = build.PerformUpgrade();

        if (!result.Successful)
        {
            _logger.LogError("Upgrade failed: {error}", result.Error);
            throw result.Error;
        }
    }

}
