using System.Threading;
using Dapper;
using StockAnalysis.Api.Data;
using StockAnalysis.Api.Models;

namespace StockAnalysis.Api.Services;

/// <summary>
/// 信息来源目录 CRUD 与文档批量导入。Schema 仍以 SQL 迁移脚本为唯一来源：
/// 首次使用时跑一次迁移器（幂等），避免客户端忘记先调用 /api/migrate 导致表不存在。
/// 批量导入以 (category, entry_type, sort_order) 为幂等键 UPSERT，重复导入不产生重复行。
/// </summary>
public sealed class InfoSourceService(Db db, MigrationRunner runner)
{
    private static readonly string[] ValidEntryTypes = ["site", "combo", "template", "note"];

    private int _ensured;

    private async Task EnsureAsync()
    {
        if (Interlocked.Exchange(ref _ensured, 1) == 0)
            await runner.MigrateAsync();
    }

    private const string SelectColumns = """
        id AS Id, category AS Category, entry_type AS EntryType, site_name AS SiteName,
        description AS Description, key_content AS KeyContent, url AS Url,
        sort_order AS SortOrder, created_at AS CreatedAt, updated_at AS UpdatedAt
        """;

    public async Task<IReadOnlyList<InfoSource>> ListAsync(string? category = null, CancellationToken ct = default)
    {
        await EnsureAsync();
        await using var conn = db.Open();
        var sql = $"""
            SELECT {SelectColumns} FROM info_source
            WHERE (@Category IS NULL OR category = @Category)
            ORDER BY category, entry_type, sort_order, id
            """;
        var rows = await conn.QueryAsync<InfoSource>(new CommandDefinition(
            sql, new { Category = string.IsNullOrWhiteSpace(category) ? null : category }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<InfoSource?> GetAsync(long id, CancellationToken ct = default)
    {
        await EnsureAsync();
        await using var conn = db.Open();
        var sql = $"SELECT {SelectColumns} FROM info_source WHERE id = @Id";
        return await conn.QuerySingleOrDefaultAsync<InfoSource>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<long> CreateAsync(InfoSourceInput input, CancellationToken ct = default)
    {
        var (category, entryType, name, desc, key, url) = Normalize(input);
        await EnsureAsync();
        await using var conn = db.Open();
        // 手工新增：顺序号取同分类同类型内当前最大值 + 1，满足唯一键
        var nextOrder = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COALESCE(MAX(sort_order), 0) + 1 FROM info_source WHERE category = @Category AND entry_type = @EntryType",
            new { Category = category, EntryType = entryType }, cancellationToken: ct));
        const string insert = """
            INSERT INTO info_source(category, entry_type, site_name, description, key_content, url, sort_order)
            VALUES (@Category, @EntryType, @Name, @Desc, @Key, @Url, @SortOrder)
            """;
        await conn.ExecuteAsync(new CommandDefinition(
            insert,
            new { Category = category, EntryType = entryType, Name = name, Desc = desc, Key = key, Url = url, SortOrder = nextOrder },
            cancellationToken: ct));
        return await conn.ExecuteScalarAsync<long>(
            new CommandDefinition("SELECT LAST_INSERT_ID()", cancellationToken: ct));
    }

    public async Task<bool> UpdateAsync(long id, InfoSourceInput input, CancellationToken ct = default)
    {
        var (category, entryType, name, desc, key, url) = Normalize(input);
        await EnsureAsync();
        await using var conn = db.Open();
        const string sql = """
            UPDATE info_source
            SET category = @Category, entry_type = @EntryType, site_name = @Name,
                description = @Desc, key_content = @Key, url = @Url
            WHERE id = @Id
            """;
        var affected = await conn.ExecuteAsync(new CommandDefinition(
            sql, new { Id = id, Category = category, EntryType = entryType, Name = name, Desc = desc, Key = key, Url = url },
            cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        await EnsureAsync();
        await using var conn = db.Open();
        const string sql = "DELETE FROM info_source WHERE id = @Id";
        var affected = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
        return affected > 0;
    }

    /// <summary>按 (category, entry_type, sort_order) 幂等批量导入，返回插入/更新计数。</summary>
    public async Task<InfoSourceImportResult> ImportAsync(
        IReadOnlyList<InfoSourceImportItem> items, CancellationToken ct = default)
    {
        await EnsureAsync();
        var result = new InfoSourceImportResult { Received = items.Count };
        await using var conn = db.Open();
        var before = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM info_source", cancellationToken: ct));
        await using var tx = await conn.BeginTransactionAsync(ct);
        const string upsert = """
            INSERT INTO info_source(category, entry_type, site_name, description, key_content, url, sort_order)
            VALUES (@Category, @EntryType, @Name, @Description, @KeyContent, @Url, @SortOrder)
            ON DUPLICATE KEY UPDATE
              site_name = VALUES(site_name),
              description = VALUES(description),
              key_content = VALUES(key_content),
              url = VALUES(url)
            """;
        foreach (var item in items)
        {
            var (category, entryType, name, desc, key, url, sortOrder) = NormalizeImport(item);
            await conn.ExecuteAsync(new CommandDefinition(
                upsert,
                new { Category = category, EntryType = entryType, Name = name, Description = desc, KeyContent = key, Url = url, SortOrder = sortOrder },
                tx, cancellationToken: ct));
        }
        await tx.CommitAsync(ct);
        // MySqlConnector 对 ON DUPLICATE KEY 的受影响行数与插入/更新不一一对应，
        // 因此用导入前后的总行数差值统计：新增 = after - before，其余视为更新。
        var after = result.Total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM info_source", cancellationToken: ct));
        result.Inserted = after - before;
        result.Updated = result.Received - result.Inserted;
        return result;
    }

    private static (string, string, string, string, string?, string?) Normalize(InfoSourceInput input)
    {
        var category = (input.Category ?? "").Trim();
        var entryType = (input.EntryType ?? "site").Trim().ToLowerInvariant();
        var name = (input.SiteName ?? "").Trim();
        var desc = (input.Description ?? "").Trim();
        var key = NullIfEmpty((input.KeyContent ?? "").Trim());
        var url = (input.Url ?? "").Trim();

        if (entryType.Length == 0) entryType = "site";
        if (!ValidEntryTypes.Contains(entryType))
            throw new ArgumentException($"条目类型仅支持：{string.Join("/", ValidEntryTypes)}");
        if (name.Length == 0) throw new ArgumentException("名称不能为空");
        if (name.Length > 100) throw new ArgumentException("名称不能超过 100 个字符");
        if (category.Length > 100) throw new ArgumentException("分类不能超过 100 个字符");
        if (desc.Length > 500) throw new ArgumentException("定位描述不能超过 500 个字符");
        // site 必填网址（ValidateUrl 内为空即抛）；其他类型允许无网址（返回 null）
        url = ValidateUrl(entryType, url)!;
        return (category, entryType, name, desc, key, url);
    }

    private static (string, string, string, string, string?, string?, int) NormalizeImport(InfoSourceImportItem item)
    {
        var entryType = (item.EntryType ?? "site").Trim().ToLowerInvariant();
        if (entryType.Length == 0) entryType = "site";
        if (!ValidEntryTypes.Contains(entryType))
            throw new ArgumentException($"条目类型仅支持：{string.Join("/", ValidEntryTypes)}");
        var category = (item.Category ?? "").Trim();
        var name = (item.Name ?? "").Trim();
        var desc = (item.Description ?? "").Trim();
        var key = NullIfEmpty((item.KeyContent ?? "").Trim());
        var url = (item.Url ?? "").Trim();
        if (name.Length == 0) throw new ArgumentException($"导入条目缺少名称（category={category}, sortOrder={item.SortOrder}）");
        if (name.Length > 100) throw new ArgumentException($"导入条目名称超长（{name}）");
        if (category.Length > 100) throw new ArgumentException($"导入条目分类超长（{category}）");
        if (desc.Length > 500) throw new ArgumentException($"导入条目描述超长（{name}）");
        url = ValidateUrl(entryType, url);
        return (category, entryType, name, desc, key, url, item.SortOrder);
    }

    /// <summary>校验 URL：site 必填且合法；其他类型可为空（返回 null），给了则必须合法。</summary>
    private static string? ValidateUrl(string entryType, string url)
    {
        if (entryType == "site")
        {
            if (url.Length == 0) throw new ArgumentException("网站条目的网页地址不能为空");
        }
        else if (url.Length == 0)
        {
            return null; // 非网站条目无网址 → 存 NULL
        }

        if (url.Length > 1024) throw new ArgumentException("网页地址不能超过 1024 个字符");
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("网页地址需以 http:// 或 https:// 开头");
        return url;
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
