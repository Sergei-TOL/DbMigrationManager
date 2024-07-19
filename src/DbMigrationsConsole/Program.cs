using System.Diagnostics;
using DbMigrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DbMigrationsConsole;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting database migrations");

        var connectionStringService = host.Services.GetRequiredService<IDbConnectionStringService>();
        var dbMigrationsService = host.Services.GetRequiredService<IDbMigrationsService>();
        var appSettingsOptions = host.Services.GetRequiredService<IOptions<AppSettings>>();

        var migrationsScriptsPath = appSettingsOptions.Value.MigrationsScriptsPath;
        var idempotentScriptsPath = appSettingsOptions.Value.IdempotentScriptsPath;

        logger.LogInformation("Migrations scripts path: {migrationsScriptsPath}", migrationsScriptsPath);
        logger.LogInformation("Idempotent scripts path: {idempotentScriptsPath}", idempotentScriptsPath);

        var absoluteMigrationsScriptsPath = PathHelper.ResolvePath(migrationsScriptsPath);
        var absoluteIdempotentScriptsPath = PathHelper.ResolvePath(idempotentScriptsPath);

        logger.LogInformation("Absolute migrations scripts path: {absoluteMigrationsScriptsPath}", absoluteMigrationsScriptsPath);
        logger.LogInformation("Absolute idempotent scripts path: {absoluteIdempotentScriptsPath}", absoluteIdempotentScriptsPath);

        var connectionString = await connectionStringService.GetConnectionStringAsync();

        await dbMigrationsService.EnsureDatabaseMigration(connectionString, absoluteMigrationsScriptsPath, absoluteIdempotentScriptsPath);
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddTransient<IDbMigrationsService, DbMigrationsService>();
                services.AddTransient<IIdempotentScriptService, IdempotentScriptService>();
                services.AddTransient<IDbConnectionStringService, DbConnectionStringService>();

                services.Configure<AppSettings>(
                    context.Configuration.GetSection(nameof(AppSettings)));
            })
        ;
}
