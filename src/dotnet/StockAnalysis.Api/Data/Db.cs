using Dapper;
using MySqlConnector;

namespace StockAnalysis.Api.Data;

public sealed class Db(IConfiguration config)
{
    private readonly string _adminConnectionString = BuildAdminConnectionString(
        config.GetConnectionString("MySQL")
        ?? throw new InvalidOperationException("缺少 ConnectionStrings:MySQL 配置"));

    private readonly string _database = new MySqlConnectionStringBuilder(
        config.GetConnectionString("MySQL")!).Database;

    private readonly string _connectionString = config.GetConnectionString("MySQL")!;

    private static string BuildAdminConnectionString(string cs)
    {
        var b = new MySqlConnectionStringBuilder(cs) { Database = "" };
        return b.ConnectionString;
    }

    public MySqlConnection Open()
    {
        var conn = new MySqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public async Task EnsureDatabaseAsync()
    {
        await using var admin = new MySqlConnection(_adminConnectionString);
        await admin.OpenAsync();
        await admin.ExecuteAsync(
            $"CREATE DATABASE IF NOT EXISTS `{_database}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
    }

}
