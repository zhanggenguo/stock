using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;

namespace StockAnalysis.Api.Data;

public sealed record MigrationScript(int Version, string Name, string Sql, string Checksum);

public sealed record MigrationResult(int Version, string Name, string Status, int ExecutionMs, string? Error);

public sealed record MigrationReport
{
    public List<MigrationResult> Results { get; init; } = [];
    public bool HasError => Results.Any(x => x.Error is not null);
    public int AppliedCount => Results.Count(x => x.Status == "applied");
    public int SkippedCount => Results.Count(x => x.Status == "skipped");
}

public sealed record MigrationInfo(int Version, string Name, string Checksum, string State, string? AppliedAt);

/// <summary>
/// SQL 脚本驱动的版本化迁移：脚本编译为嵌入式资源随程序发布，
/// 已应用脚本以 checksum 锁定，禁止修改旧版本，只能新增版本升级。
/// </summary>
public sealed class MigrationRunner(
    Db db, IConfiguration config, ILogger<MigrationRunner> logger)
{
    private const string ResourcePrefix = "StockAnalysis.Api.Db.Migrations.";
    private const string MetaTableSql = """
        CREATE TABLE IF NOT EXISTS schema_migrations (
          version INT NOT NULL PRIMARY KEY,
          name VARCHAR(128) NOT NULL,
          checksum CHAR(64) NOT NULL,
          applied_at DATETIME(6) NOT NULL,
          execution_ms INT NOT NULL DEFAULT 0
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='数据库迁移版本记录'
        """;

    private static readonly Regex FilePattern = new(
        @"^V(?<v>\d{3,})__(?<n>.+)\.sql$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private sealed record AppliedRow(int Version, string Name, string Checksum, DateTime AppliedAt, int ExecutionMs);

    public async Task<MigrationReport> MigrateAsync(CancellationToken ct = default)
    {
        var report = new MigrationReport();
        await db.EnsureDatabaseAsync();
        await using var conn = db.Open();
        await conn.ExecuteAsync(MetaTableSql);

        var applied = (await conn.QueryAsync<AppliedRow>(
            "SELECT version AS Version, name AS Name, checksum AS Checksum, applied_at AS AppliedAt, execution_ms AS ExecutionMs FROM schema_migrations ORDER BY version"))
            .ToDictionary(x => x.Version);

        foreach (var script in LoadScripts())
        {
            ct.ThrowIfCancellationRequested();
            if (applied.TryGetValue(script.Version, out var row))
            {
                if (!string.Equals(row.Checksum, script.Checksum, StringComparison.OrdinalIgnoreCase))
                {
                    var msg = $"V{script.Version:D3} 脚本内容已变更（已应用 {row.Checksum[..12]}… / 当前 {script.Checksum[..12]}…）。" +
                              "已应用版本禁止修改，请新增 V 版本脚本。";
                    report.Results.Add(new MigrationResult(script.Version, script.Name, "failed", 0, msg));
                    logger.LogError("{Msg}", msg);
                    break;
                }
                report.Results.Add(new MigrationResult(script.Version, script.Name, "skipped", row.ExecutionMs, null));
                continue;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                foreach (var statement in SplitStatements(script.Sql))
                    await conn.ExecuteAsync(new CommandDefinition(statement, cancellationToken: ct));
                sw.Stop();
                await conn.ExecuteAsync(
                    "INSERT INTO schema_migrations(version,name,checksum,applied_at,execution_ms) VALUES (@Version,@Name,@Checksum,@AppliedAt,@ExecutionMs)",
                    new { script.Version, script.Name, script.Checksum, AppliedAt = DateTime.UtcNow, ExecutionMs = (int)sw.ElapsedMilliseconds });
                report.Results.Add(new MigrationResult(script.Version, script.Name, "applied", (int)sw.ElapsedMilliseconds, null));
                logger.LogInformation("迁移 V{Version:D3} {Name} 完成，耗时 {Ms}ms", script.Version, script.Name, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                report.Results.Add(new MigrationResult(script.Version, script.Name, "failed", (int)sw.ElapsedMilliseconds, ex.Message));
                logger.LogError(ex, "迁移 V{Version:D3} {Name} 失败", script.Version, script.Name);
                break;
            }
        }
        return report;
    }

    public async Task<IReadOnlyList<MigrationInfo>> StatusAsync()
    {
        await db.EnsureDatabaseAsync();
        await using var conn = db.Open();
        await conn.ExecuteAsync(MetaTableSql);
        var applied = (await conn.QueryAsync<AppliedRow>(
            "SELECT version AS Version, name AS Name, checksum AS Checksum, applied_at AS AppliedAt, execution_ms AS ExecutionMs FROM schema_migrations ORDER BY version"))
            .ToDictionary(x => x.Version);

        return LoadScripts().Select(script =>
        {
            if (!applied.TryGetValue(script.Version, out var row))
                return new MigrationInfo(script.Version, script.Name, script.Checksum, "pending", null);
            var state = string.Equals(row.Checksum, script.Checksum, StringComparison.OrdinalIgnoreCase)
                ? "applied" : "modified";
            return new MigrationInfo(script.Version, script.Name, script.Checksum, state, row.AppliedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        }).ToList();
    }

    private List<MigrationScript> LoadScripts()
    {
        var scripts = new List<MigrationScript>();
        var external = config["Database:MigrationsDirectory"];
        if (!string.IsNullOrWhiteSpace(external) && Directory.Exists(external))
        {
            foreach (var file in Directory.GetFiles(external, "*.sql").OrderBy(x => x))
                scripts.Add(Parse(Path.GetFileName(file), File.ReadAllText(file, Encoding.UTF8)));
        }
        else
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var resource in asm.GetManifestResourceNames()
                         .Where(x => x.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                         .OrderBy(x => x))
            {
                using var stream = asm.GetManifestResourceStream(resource)
                    ?? throw new InvalidOperationException($"无法读取迁移资源：{resource}");
                using var reader = new StreamReader(stream, Encoding.UTF8);
                scripts.Add(Parse(resource[ResourcePrefix.Length..], reader.ReadToEnd()));
            }
        }

        var duplicates = scripts.GroupBy(x => x.Version).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException($"迁移版本号重复：{string.Join(", ", duplicates)}");
        return scripts.OrderBy(x => x.Version).ToList();
    }

    private static MigrationScript Parse(string fileName, string sql)
    {
        var match = FilePattern.Match(fileName);
        if (!match.Success)
            throw new InvalidOperationException($"迁移文件命名不合法：{fileName}，应为 V001__name.sql");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql))).ToLowerInvariant();
        return new MigrationScript(int.Parse(match.Groups["v"].Value), match.Groups["n"].Value, sql, hash);
    }

    private static IEnumerable<string> SplitStatements(string sql)
    {
        foreach (var part in sql.Split(';'))
        {
            var meaningful = part.Split('\n')
                .Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal))
                .Select(line => line.Trim());
            if (string.Concat(meaningful).Length == 0) continue;
            yield return part.Trim();
        }
    }
}
