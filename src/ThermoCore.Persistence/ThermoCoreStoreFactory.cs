namespace ThermoCore.Persistence;

/// <summary>Creates <see cref="IThermoCoreStore"/> instances for SQLite or PostgreSQL.</summary>
public static class ThermoCoreStoreFactory
{
    public static IThermoCoreStore Create(ThermoCoreStoreProvider provider, string connectionOrPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionOrPath);
        return provider switch
        {
            ThermoCoreStoreProvider.Sqlite => new SqliteThermoCoreStore(connectionOrPath),
            ThermoCoreStoreProvider.PostgreSql => new PostgresThermoCoreStore(connectionOrPath),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unsupported store provider.")
        };
    }

    /// <summary>
    /// Parses <c>sqlite:path</c> or <c>postgres:connection-string</c> (also <c>postgresql:</c>).
    /// Bare paths default to SQLite.
    /// </summary>
    public static IThermoCoreStore CreateFromSpecifier(string specifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(specifier);
        const string sqlitePrefix = "sqlite:";
        const string postgresPrefix = "postgres:";
        const string postgresqlPrefix = "postgresql:";

        if (specifier.StartsWith(sqlitePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Create(ThermoCoreStoreProvider.Sqlite, specifier[sqlitePrefix.Length..]);
        }

        if (specifier.StartsWith(postgresPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Create(ThermoCoreStoreProvider.PostgreSql, specifier[postgresPrefix.Length..]);
        }

        if (specifier.StartsWith(postgresqlPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Create(ThermoCoreStoreProvider.PostgreSql, specifier[postgresqlPrefix.Length..]);
        }

        return Create(ThermoCoreStoreProvider.Sqlite, specifier);
    }
}
