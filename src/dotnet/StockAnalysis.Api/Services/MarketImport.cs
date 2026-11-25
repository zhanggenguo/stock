using System.Globalization;
using System.Text.Json;
using Dapper;
using StockAnalysis.Api.Data;
using StockAnalysis.Api.Models;

namespace StockAnalysis.Api.Services;

/// <summary>从 data/raw/market 的原始 JSON 导入日市值快照，按 (company_id, trade_date) 幂等 UPSERT。</summary>
public sealed class MarketImport(Db db, ILogger<MarketImport> logger)
{
    private static readonly string[] SkipFiles = ["manifest.json", "fetch_log.json"];

    public async Task<MarketImportResult> ImportAsync(string rawDirectory, string? tradeDate = null,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(rawDirectory))
            throw new DirectoryNotFoundException($"市值原始目录不存在：{rawDirectory}");

        var result = new MarketImportResult { TradeDate = tradeDate };
        var batch = new List<MarketCap>(500);
        await using var conn = db.Open();

        foreach (var file in Directory.EnumerateFiles(rawDirectory, "*.json")
                     .Where(x => !SkipFiles.Contains(Path.GetFileName(x)))
                     .OrderBy(x => x))
        {
            ct.ThrowIfCancellationRequested();
            result.Scanned++;
            try
            {
                using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(file, ct));
                var root = doc.RootElement;
                var companyId = S(root, "company_id") ?? "";
                var exchange = S(root, "exchange") ?? "";
                if (string.IsNullOrEmpty(companyId) ||
                    !root.TryGetProperty("items", out var items) ||
                    items.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var item in items.EnumerateArray())
                {
                    var dateText = S(item, "trade_date");
                    if (string.IsNullOrEmpty(dateText)) continue;
                    if (!DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var date))
                        continue;
                    if (tradeDate is not null && date.ToString("yyyy-MM-dd") != tradeDate)
                        continue;
                    result.Rows++;
                    batch.Add(new MarketCap
                    {
                        CompanyId = companyId,
                        TradeDate = date,
                        Exchange = exchange,
                        Close = D(item, "close") ?? 0m,
                        PctChange = D(item, "pct_change"),
                        Amplitude = D(item, "amplitude"),
                        Volume = D(item, "volume"),
                        Amount = D(item, "amount"),
                        TurnoverRate = D(item, "turnover_rate"),
                        TotalShares = D(item, "total_shares"),
                        FloatShares = D(item, "float_shares"),
                        TotalMarketCap = D(item, "total_market_cap") ?? 0m,
                        FloatMarketCap = D(item, "float_market_cap"),
                        PeTtm = D(item, "pe_ttm"),
                        Pb = D(item, "pb"),
                        IsSuspended = I(item, "is_suspended") == 1,
                        SharesAsOf = ParseDate(S(item, "shares_asof")),
                        QualityFlag = S(item, "quality_flag") ?? "",
                        Source = S(item, "source") ?? "",
                        SourceUrl = S(item, "source_url"),
                        RawHash = S(root, "raw_hash"),
                        FetchedAt = S(item, "fetched_at") ?? S(root, "fetched_at") ?? "",
                    });
                    result.Written++;
                    if (batch.Count >= 500)
                    {
                        await UpsertBatchAsync(conn, batch);
                        batch.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
                logger.LogWarning(ex, "市值原始文件解析失败 {File}", file);
            }
        }
        if (batch.Count > 0) await UpsertBatchAsync(conn, batch);
        return result;
    }

    private static async Task UpsertBatchAsync(MySqlConnector.MySqlConnection conn, IEnumerable<MarketCap> rows)
    {
        const string sql = """
            INSERT INTO fact_market_cap_daily (
              company_id, trade_date, exchange, close, pct_change, amplitude, volume, amount,
              turnover_rate, total_shares, float_shares, total_market_cap, float_market_cap,
              pe_ttm, pe_static, pe_dynamic, pb, ps_ttm, dividend_yield, is_suspended,
              shares_asof, quality_flag, source, source_url, raw_hash, fetched_at
            ) VALUES (
              @CompanyId, @TradeDate, @Exchange, @Close, @PctChange, @Amplitude, @Volume, @Amount,
              @TurnoverRate, @TotalShares, @FloatShares, @TotalMarketCap, @FloatMarketCap,
              @PeTtm, NULL, NULL, @Pb, NULL, NULL, @IsSuspended,
              @SharesAsOf, @QualityFlag, @Source, @SourceUrl, @RawHash, @FetchedAt
            )
            ON DUPLICATE KEY UPDATE
              exchange=VALUES(exchange), close=VALUES(close), pct_change=VALUES(pct_change),
              amplitude=VALUES(amplitude), volume=VALUES(volume), amount=VALUES(amount),
              turnover_rate=VALUES(turnover_rate), total_shares=VALUES(total_shares),
              float_shares=VALUES(float_shares), total_market_cap=VALUES(total_market_cap),
              float_market_cap=VALUES(float_market_cap), pe_ttm=VALUES(pe_ttm),
              pb=VALUES(pb), is_suspended=VALUES(is_suspended), shares_asof=VALUES(shares_asof),
              quality_flag=VALUES(quality_flag), source=VALUES(source),
              source_url=VALUES(source_url), raw_hash=VALUES(raw_hash), fetched_at=VALUES(fetched_at)
            """;
        await conn.ExecuteAsync(sql, rows);
    }

    private static string? S(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) &&
        v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static decimal? D(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) &&
        v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;

    private static int? I(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) &&
        v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;

    private static DateTime? ParseDate(string? text) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var d) ? d : null;
}
