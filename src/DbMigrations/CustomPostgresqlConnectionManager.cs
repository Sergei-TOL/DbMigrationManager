using System.Security.Cryptography.X509Certificates;
using DbUp.Engine.Transactions;
using Npgsql;

namespace DbMigrations;

/// Fix when file splited into several statements by ";" character. 
/// But in Postgresql ";" is used in procedures and functions.

/// <summary>
/// Manages PostgreSQL database connections.
/// </summary>
public class CustomPostgresqlConnectionManager : DatabaseConnectionManager
{
    /// <summary>
    /// Creates a new PostgreSQL database connection.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public CustomPostgresqlConnectionManager(string connectionString)
        : base(new DelegateConnectionFactory(l => new NpgsqlConnection(connectionString)))
    {
    }

    /// <summary>
    /// Creates a new PostgreSQL database connection with a certificate.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="certificate">Certificate for securing connection.</param>
    public CustomPostgresqlConnectionManager(string connectionString, X509Certificate2 certificate)
        : base(new DelegateConnectionFactory(l =>
        {
            NpgsqlConnection databaseConnection = new NpgsqlConnection(connectionString);
            databaseConnection.ProvideClientCertificatesCallback +=
                    certs => certs.Add(certificate);

            return databaseConnection;
        }))
    {
    }

    /// <summary>
    /// Splits the statements in the script using the ";" character.
    /// </summary>
    /// <param name="scriptContents">The contents of the script to split.</param>
    public override IEnumerable<string> SplitScriptIntoCommands(string scriptContents)
    {
        // ignore spliting
        return [scriptContents];
    }
}
