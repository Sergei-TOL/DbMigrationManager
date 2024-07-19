using System.Diagnostics;
using DbMigrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DbMigrationsConsole;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        var connectionStringService = host.Services.GetRequiredService<IDbConnectionStringService>();
        var dbMigrationsService = host.Services.GetRequiredService<IDbMigrationsService>();
        var appSettingsOptions = host.Services.GetRequiredService<IOptions<AppSettings>>();

        var migrationsScriptsPath = PathHelper.ResolvePath(appSettingsOptions.Value.MigrationsScriptsPath);
        var idempotentScriptsPath = PathHelper.ResolvePath(appSettingsOptions.Value.IdempotentScriptsPath);


        var connectionString = await connectionStringService.GetConnectionStringAsync();

        await dbMigrationsService.EnsureDatabaseMigration(connectionString, migrationsScriptsPath, idempotentScriptsPath);
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
