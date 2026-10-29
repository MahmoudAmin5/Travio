using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Travio.Infrastructure;

namespace Travio.UnitTests.Fixtures;

/// <summary>
/// Relational SQLite in-memory DbContext subclass of <see cref="ApplicationDbContext"/>
/// that supports real transactions, Savepoints (SP_Depth / SP_Ambient), ExecuteUpdateAsync,
/// ExecuteDeleteAsync, and SQL .Select(...) projections while adapting SQL Server-specific
/// rowversion / DefaultValueSql metadata for SQLite.
/// </summary>
public class SqliteTestDbContext : ApplicationDbContext
{
    public SqliteTestDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                // Adapt SQL Server rowversion columns for SQLite so inserts succeed without SQL Server's auto-generated 8-byte timestamp
                if (property.IsConcurrencyToken && property.ClrType == typeof(byte[]))
                {
                    property.ValueGenerated = ValueGenerated.Never;
                    property.SetDefaultValue(new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 });
                }

                // Adapt any SQL Server-only DefaultValueSql expressions (e.g. GETUTCDATE())
                var defaultSql = property.GetDefaultValueSql();
                if (!string.IsNullOrEmpty(defaultSql) &&
                    defaultSql.Contains("GETUTCDATE", StringComparison.OrdinalIgnoreCase))
                {
                    property.SetDefaultValueSql("CURRENT_TIMESTAMP");
                }

                // Enable SQLite ORDER BY support on DateTimeOffset / DateTimeOffset? columns
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(new DateTimeOffsetToBinaryConverter());
                }
            }
        }
    }
}

public sealed class SqliteTestDatabase : IAsyncDisposable, IDisposable
{
    public SqliteConnection Connection { get; }
    public SqliteTestDbContext Context { get; }

    public SqliteTestDatabase()
    {
        Connection = new SqliteConnection("Filename=:memory:");
        Connection.Open();

        // Disable foreign key enforcement when tests seed partial graphs unless explicitly needed
        using (var cmd = Connection.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
            cmd.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(Connection)
            .Options;

        Context = new SqliteTestDbContext(options);
        Context.Database.EnsureCreated();
    }

    public SqliteTestDbContext CreateNewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(Connection)
            .Options;

        return new SqliteTestDbContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
        Connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
