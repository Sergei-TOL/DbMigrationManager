namespace DbMigrationsConsole;

public class AppSettings
{
    public required ConnectionStringData ConnectionStringData { get; set; }
    public AwsSecretManagerSettings? AwsSecretManager { get; set; }
    public required string MigrationsScriptsPath { get; set; }
    public required string IdempotentScriptsPath { get; set; }
}

public class ConnectionStringData
{
    public required string Host { get; set; }
    public required string Port { get; set; }
    public required string Database { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class AwsSecretManagerSettings
{
    public required string Region { get; set; }
    public required string DbCredentialsAccessKey { get; set; }
}
