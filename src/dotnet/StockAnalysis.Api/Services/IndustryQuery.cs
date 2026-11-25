using Dapper;
using StockAnalysis.Api.Data;
using StockAnalysis.Api.Models;

namespace StockAnalysis.Api.Services;

/// <summary>
/// 行业树查询与重建。SQL 回灌段与 V003__industry_em.sql 保持一致，
/// 修改 V003 必须新增 V004 并同步本文件的 RebuildStatements。
/// </summary>
public sealed class IndustryQuery(Db db)
{
    public const string SupportedScheme = "EM";

    private static readonly string[] RebuildStatements =
    [
        "DELETE FROM company_industry_map WHERE scheme = 'EM'",
        "DELETE FROM dim_industry WHERE scheme = 'EM'",
        """
        INSERT INTO dim_industry (industry_id, scheme, level, parent_id, name, path)
        SELECT DISTINCT CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1)), 'EM', 1, NULL,
               SUBSTRING_INDEX(industry_em, '-', 1), SUBSTRING_INDEX(industry_em, '-', 1)
        FROM company
        WHERE industry_em IS NOT NULL AND industry_em <> '' AND industry_em LIKE '%-%-%'
        """,
        """
        INSERT INTO dim_industry (industry_id, scheme, level, parent_id, name, path)
        SELECT DISTINCT
               CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1), '/', SUBSTRING_INDEX(SUBSTRING_INDEX(industry_em, '-', 2), '-', -1)),
               'EM', 2,
               CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1)),
               SUBSTRING_INDEX(SUBSTRING_INDEX(industry_em, '-', 2), '-', -1),
               SUBSTRING_INDEX(industry_em, '-', 2)
        FROM company
        WHERE industry_em IS NOT NULL AND industry_em <> '' AND industry_em LIKE '%-%-%'
        """,
        """
        INSERT INTO dim_industry (industry_id, scheme, level, parent_id, name, path)
        SELECT DISTINCT
               CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1), '/',
                      SUBSTRING_INDEX(SUBSTRING_INDEX(industry_em, '-', 2), '-', -1), '/',
                      SUBSTRING_INDEX(industry_em, '-', -1)),
               'EM', 3,
               CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1), '/',
                      SUBSTRING_INDEX(SUBSTRING_INDEX(industry_em, '-', 2), '-', -1)),
               SUBSTRING_INDEX(industry_em, '-', -1),
               industry_em
        FROM company
        WHERE industry_em IS NOT NULL AND industry_em <> '' AND industry_em LIKE '%-%-%'
        """,
        """
        INSERT INTO company_industry_map (company_id, industry_id, scheme, is_primary, source)
        SELECT company_id,
               CONCAT('EM/', SUBSTRING_INDEX(industry_em, '-', 1), '/',
                      SUBSTRING_INDEX(SUBSTRING_INDEX(industry_em, '-', 2), '-', -1), '/',
                      SUBSTRING_INDEX(industry_em, '-', -1)),
               'EM', 1, 'EM_F10/EM2016'
        FROM company
        WHERE industry_em IS NOT NULL AND industry_em <> '' AND industry_em LIKE '%-%-%'
        """
    ];

    public async Task<IReadOnlyList<IndustryNode>> TreeAsync(string scheme)
    {
        if (scheme != SupportedScheme)
            return [];

        await using var conn = db.Open();
        var nodes = (await conn.QueryAsync<IndustryNode>(
            """
            SELECT d.industry_id AS IndustryId, d.scheme AS Scheme, d.level AS Level,
                   d.parent_id AS ParentId, d.name AS Name,
                   (SELECT COUNT(*) FROM company_industry_map m
                    WHERE m.industry_id = d.industry_id AND m.scheme = d.scheme) AS DirectCount
            FROM dim_industry d
            WHERE d.scheme = @scheme
            ORDER BY d.level, d.industry_id
            """, new { scheme })).ToList();

        // 先初始化，再自底向上累加：子节点 totalCount 并入父节点
        var byId = nodes.ToDictionary(x => x.IndustryId);
        foreach (var node in nodes)
            node.TotalCount = node.DirectCount;
        foreach (var node in nodes.OrderByDescending(x => x.Level))
            if (node.ParentId is not null && byId.TryGetValue(node.ParentId, out var parent))
                parent.TotalCount += node.TotalCount;
        return nodes;
    }

    public async Task<IndustryRebuildResult> RebuildAsync(string scheme)
    {
        if (scheme != SupportedScheme)
            throw new ArgumentException($"本期仅支持 scheme={SupportedScheme}，收到 {scheme}", nameof(scheme));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await using var conn = db.Open();
        foreach (var sql in RebuildStatements)
            await conn.ExecuteAsync(sql);
        sw.Stop();

        return new IndustryRebuildResult
        {
            Scheme = scheme,
            Industries = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dim_industry WHERE scheme = @scheme", new { scheme }),
            Mappings = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM company_industry_map WHERE scheme = @scheme", new { scheme }),
            ElapsedMs = (int)sw.ElapsedMilliseconds
        };
    }
}
