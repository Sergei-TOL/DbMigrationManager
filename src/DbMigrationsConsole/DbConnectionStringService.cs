using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DbMigrationsConsole;


public interface IDbConnectionStringService
{
    Task<string> GetConnectionStringAsync();
}

public class DbConnectionStringService : IDbConnectionStringService
{
    private readonly AppSettings _appSettings;
    private readonly ILogger<DbConnectionStringService> _logger;

    public DbConnectionStringService(
        IOptions<AppSettings> appSettings,
        ILogger<DbConnectionStringService> logger)
    {
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task<string> GetConnectionStringAsync()
    {
        var connectionData = _appSettings.ConnectionStringData;

        string? username = connectionData.Username;
        string? password = connectionData.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            if (_appSettings.AwsSecretManager == null)
            {
                throw new InvalidOperationException("AWS Secrets Manager configuration is missing, but no username/password provided.");
            }

            _logger.LogInformation("Retrieving credentials from AWS Secrets Manager.");
            var credentials = await GetAwsSecretAsync(_appSettings.AwsSecretManager.Region, _appSettings.AwsSecretManager.DbCredentialsAccessKey);
            username = credentials.Username;
            password = credentials.Password;
        }

        var connectionBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = connectionData.Host,
            Port = int.Parse(connectionData.Port),
            Database = connectionData.Database,
            Username = username,
            Password = password,
        };

        _logger.LogInformation("Connection string built from configuration and/or AWS Secrets Manager.");

        return connectionBuilder.ToString();
    }

    private async Task<(string Username, string Password)> GetAwsSecretAsync(string region, string secretKey)
    {
        AmazonSecretsManagerClient client = new(RegionEndpoint.GetBySystemName(region));
        GetSecretValueRequest request = new() { SecretId = secretKey };
        GetSecretValueResponse response = await client.GetSecretValueAsync(request);

        if (string.IsNullOrEmpty(response.SecretString))
        {
            throw new InvalidOperationException("AWS secret is empty.");
        }

        var secretDict = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
        if (secretDict == null
            || !secretDict.TryGetValue("username", out var username)
            || !secretDict.TryGetValue("password", out var password))
        {
            throw new InvalidOperationException("AWS secret does not contain required credentials.");
        }

        return (username, password);
    }
}
