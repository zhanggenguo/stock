using System.Globalization;
using Dapper;
using StockAnalysis.Api.Data;
using StockAnalysis.Api.Models;

namespace StockAnalysis.Api.Services;

public sealed class MarketQuery(Db db)
{
    public async Task<IReadOnlyList<MarketCap>> DailyAsync(string? companyId, DateTime? from,
        DateTime? to, int limit, CancellationToken ct = default)
    {
        var where = new List<string>();
        var args = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(companyId))
        {
            where.Add("company_id = @CompanyId");
            args.Add("CompanyId", companyId);
        }
        if (from.HasValue)
        {
            where.Add("trade_date >= @From");
            args.Add("From", from.Value);
        }
        if (to.HasValue)
        {
            where.Add("trade_date <= @To");
            args.Add("To", to.Value);
        }
        var clause = where.Count == 0 ? "" : " WHERE " + string.Join(" AND ", where);
        args.Add("Limit", Math.Clamp(limit, 1, 20000));
        var sql = $"""
            SELECT id AS Id, company_id AS CompanyId, trade_date AS TradeDate, exchange AS Exchange,
                   close AS Close, pct_change AS PctChange, amplitude AS Amplitude,
                   volume AS Volume, amount AS Amount, turnover_rate AS TurnoverRate,
                   total_shares AS TotalShares, float_shares AS FloatShares,
                   total_market_cap AS TotalMarketCap, float_market_cap AS FloatMarketCap,
                   pe_ttm AS PeTtm, pe_static AS PeStatic, pe_dynamic AS PeDynamic,
                   pb AS Pb, ps_ttm AS PsTtm, dividend_yield AS DividendYield,
                   is_suspended AS IsSuspended, shares_asof AS SharesAsOf,
                   quality_flag AS QualityFlag, source AS Source, source_url AS SourceUrl,
                   raw_hash AS RawHash, fetched_at AS FetchedAt
            FROM fact_market_cap_daily
            {clause}
            ORDER BY trade_date DESC, company_id
            LIMIT @Limit
            """;
        await using var conn = db.Open();
        var rows = await conn.QueryAsync<MarketCap>(sql, args);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<MarketDayStatus>> StatusAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT trade_date AS TradeDate, COUNT(*) AS RowCount
            FROM fact_market_cap_daily
            GROUP BY trade_date
            ORDER BY trade_date DESC
            """;
        await using var conn = db.Open();
        return (await conn.QueryAsync<MarketDayStatus>(sql)).ToList();
    }

    public async Task<MarketStats> StatsAsync(DateTime? tradeDate, CancellationToken ct = default)
    {
        var date = tradeDate ?? await LatestDateAsync(ct);
        const string sql = """
            SELECT @Date AS TradeDate,
                   COUNT(*) AS Companies,
                   SUM(total_market_cap) AS TotalMarketCap,
                   SUM(float_market_cap) AS FloatMarketCap
            FROM fact_market_cap_daily
            WHERE trade_date = @Date
            """;
        await using var conn = db.Open();
        var stats = await conn.QuerySingleAsync<MarketStats>(sql, new { Date = date.Date });

        const string medianSql = """
            SELECT AVG(total_market_cap) AS MedianMarketCap FROM (
              SELECT total_market_cap
              FROM fact_market_cap_daily
              WHERE trade_date = @Date
              ORDER BY total_market_cap
              LIMIT @A, @B
            ) x
            """;
        var count = stats.Companies;
        if (count > 0)
        {
            var a = (count - 1) / 2;
            var b = count % 2 == 0 ? 2 : 1;
            stats.MedianMarketCap = await conn.QuerySingleOrDefaultAsync<decimal?>(
                medianSql, new { Date = date.Date, A = a, B = b });
        }
        return stats;
    }

    private async Task<DateTime> LatestDateAsync(CancellationToken ct)
    {
        await using var conn = db.Open();
        return await conn.ExecuteScalarAsync<DateTime>(
            "SELECT COALESCE(MAX(trade_date), CURDATE()) FROM fact_market_cap_daily");
    }
}
