using Dapper;
using StockAnalysis.Api.Data;
using StockAnalysis.Api.Models;

namespace StockAnalysis.Api.Services;

public sealed class CompanyQuery(Db db)
{
    public async Task<IReadOnlyList<Company>> SearchAsync(
        string? keyword, string? exchange, int limit = 50, string? industryId = null, int? level = null)
    {
        await using var conn = db.Open();
        var parameters = new DynamicParameters();
        string sql;

        if (!string.IsNullOrWhiteSpace(industryId))
        {
            // 递归 CTE 取目标节点及全部后代，按行业 ID 或其父级路径筛选
            sql = """
                WITH RECURSIVE sub AS (
                  SELECT industry_id FROM dim_industry
                  WHERE industry_id = @industryId AND scheme = 'EM'
                  UNION ALL
                  SELECT d.industry_id FROM dim_industry d
                  JOIN sub ON d.parent_id = sub.industry_id
                )
                SELECT c.company_id AS CompanyId, c.code AS Code, c.exchange AS Exchange, c.board AS Board,
                       c.name AS Name, c.name_full AS NameFull, c.industry_em AS IndustryEm,
                       c.industry_csrc AS IndustryCsrc, c.province AS Province, c.list_date AS ListDate,
                       c.actual_controller AS ActualController, c.emp_num AS EmpNum, c.fetched_at AS FetchedAt
                FROM company c
                INNER JOIN company_industry_map m
                  ON m.company_id = c.company_id AND m.scheme = 'EM'
                WHERE m.industry_id IN (SELECT industry_id FROM sub)
                """;
            parameters.Add("industryId", industryId.Trim());
            // level 为预留参数：公司固定挂在三级叶子节点，本期不按层级二次过滤。
        }
        else
        {
            sql = """
                SELECT company_id AS CompanyId, code AS Code, exchange AS Exchange, board AS Board,
                       name AS Name, name_full AS NameFull, industry_em AS IndustryEm,
                       industry_csrc AS IndustryCsrc, province AS Province, list_date AS ListDate,
                       actual_controller AS ActualController, emp_num AS EmpNum, fetched_at AS FetchedAt
                FROM company WHERE 1=1
                """;
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var nameCol = string.IsNullOrWhiteSpace(industryId) ? "name" : "c.name";
            var codeCol = string.IsNullOrWhiteSpace(industryId) ? "code" : "c.code";
            var fullCol = string.IsNullOrWhiteSpace(industryId) ? "name_full" : "c.name_full";
            sql += $" AND ({nameCol} LIKE @kw OR {codeCol} LIKE @kw OR {fullCol} LIKE @kw)";
            parameters.Add("kw", $"%{keyword.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(exchange))
        {
            var exCol = string.IsNullOrWhiteSpace(industryId) ? "exchange" : "c.exchange";
            sql += $" AND {exCol} = @exchange";
            parameters.Add("exchange", exchange);
        }
        sql += string.IsNullOrWhiteSpace(industryId) ? " ORDER BY company_id LIMIT @limit" : " ORDER BY c.company_id LIMIT @limit";
        parameters.Add("limit", Math.Clamp(limit, 1, 500));
        return (await conn.QueryAsync<Company>(sql, parameters)).ToList();
    }

    public async Task<Company?> GetAsync(string companyId)
    {
        await using var conn = db.Open();
        return await conn.QuerySingleOrDefaultAsync<Company>(
            """
            SELECT company_id AS CompanyId, code AS Code, exchange AS Exchange, board AS Board,
                   name AS Name, name_full AS NameFull, name_en AS NameEn, former_names AS FormerNames,
                   list_date AS ListDate, found_date AS FoundDate, industry_em AS IndustryEm,
                   industry_csrc AS IndustryCsrc, province AS Province, legal_person AS LegalPerson,
                   chairman AS Chairman, president AS President, secretary AS Secretary,
                   actual_controller AS ActualController, reg_capital_wan AS RegCapitalWan,
                   emp_num AS EmpNum, auditor AS Auditor, law_firm AS LawFirm, website AS Website,
                   tel AS Tel, email AS Email, address AS Address, reg_address AS RegAddress,
                   issue_price AS IssuePrice, par_value AS ParValue, total_issue_num AS TotalIssueNum,
                   profile AS Profile, business_scope AS BusinessScope, source_url AS SourceUrl,
                   fetched_at AS FetchedAt, raw_hash AS RawHash
            FROM company WHERE company_id = @id
            """, new { id = companyId });
    }

    public async Task<object> StatsAsync()
    {
        await using var conn = db.Open();
        var rows = await conn.QueryAsync("""
            SELECT exchange, COUNT(*) AS count FROM company GROUP BY exchange
            """);
        var total = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM company");
        var latest = await conn.ExecuteScalarAsync<string?>("SELECT MAX(fetched_at) FROM company");
        return new
        {
            total,
            latestFetchedAt = latest,
            byExchange = rows.Select(r => new { exchange = (string)r.exchange, count = (int)r.count }).ToList()
        };
    }
}
