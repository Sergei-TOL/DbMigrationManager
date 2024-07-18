using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DbMigrations;

public interface IIdempotentScriptService
{
    Task ApplyIdempotentScripts(string connectionString, string scriptsPath);
}

public class IdempotentScriptService(ILogger<IdempotentScriptService> logger) : IIdempotentScriptService
{
    private readonly ILogger<IdempotentScriptService> _logger = logger;
    private readonly string _journalTableName = "public.__idempotent_scripts_journal";

    public async Task ApplyIdempotentScripts(string connectionString, string scriptsPath)
    {
        _logger.LogDebug("Applying idempotent scripts from {scriptsPath}", scriptsPath);

        var scripts = Directory.GetFiles(scriptsPath, "*.sql", SearchOption.AllDirectories);

        if (scripts.Length == 0)
        {
            _logger.LogDebug("No idempotent scripts found in {scriptsPath}", scriptsPath);
            return;
        }

        _logger.LogDebug("Found {scriptCount} idempotent scripts: {scripts}", scripts.Length, scripts);

        await EnsureIdempotentScriptsTableExists(connectionString);

        foreach (var script in scripts)
        {
            await ApplyIdempotentScript(connectionString, script);
        }

        _logger.LogInformation("All idempotent scripts applied");
    }

    private async Task EnsureIdempotentScriptsTableExists(string connectionString)
    {
        string createTableSql = @$"
            CREATE TABLE IF NOT EXISTS {_journalTableName} (
                script_name TEXT PRIMARY KEY,
                script_hash TEXT NOT NULL,
                last_applied_at TIMESTAMP NOT NULL
            )";

        using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync(createTableSql);
    }

    private async Task ApplyIdempotentScript(string connectionString, string scriptPath)
    {
        var scriptName = Path.GetFileName(scriptPath);
        var scriptContent = await File.ReadAllTextAsync(scriptPath);
        var scriptHash = ComputeHash(scriptContent);

        using var connection = new NpgsqlConnection(connectionString);

        var existingScript = await connection.QueryFirstOrDefaultAsync<IdempotentScriptRecord>(
            $"SELECT * FROM {_journalTableName} WHERE script_name = @ScriptName",
            new { ScriptName = scriptName });

        if (existingScript == null || existingScript.ScriptHash != scriptHash)
        {
            _logger.LogInformation("Applying idempotent script: {ScriptName}", scriptName);

            await connection.ExecuteAsync(scriptContent);

            await connection.ExecuteAsync(@$"
                INSERT INTO {_journalTableName} (script_name, script_hash, last_applied_at)
                VALUES (@ScriptName, @ScriptHash, @LastAppliedAt)
                ON CONFLICT (script_name) DO UPDATE
                SET script_hash = @ScriptHash, last_applied_at = @LastAppliedAt",
                new
                {
                    ScriptName = scriptName,
                    ScriptHash = scriptHash,
                    LastAppliedAt = DateTime.UtcNow
                });

            _logger.LogInformation("Idempotent script applied: {ScriptName}", scriptName);
        }
        else
        {
            _logger.LogDebug("Idempotent script unchanged, skipping: {ScriptName}", scriptName);
        }
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

public class IdempotentScriptRecord
{
    public required string ScriptName { get; set; }
    public required string ScriptHash { get; set; }
    public DateTime LastApplied { get; set; }
}
