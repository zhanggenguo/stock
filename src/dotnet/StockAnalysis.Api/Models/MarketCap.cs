namespace StockAnalysis.Api.Models;

/// <summary>公司日市值与估值快照，对应 fact_market_cap_daily。</summary>
public sealed class MarketCap
{
    public long Id { get; set; }
    public string CompanyId { get; set; } = "";
    public DateTime TradeDate { get; set; }
    public string Exchange { get; set; } = "";

    public decimal Close { get; set; }
    public decimal? PctChange { get; set; }
    public decimal? Amplitude { get; set; }
    public decimal? Volume { get; set; }
    public decimal? Amount { get; set; }
    public decimal? TurnoverRate { get; set; }

    public decimal? TotalShares { get; set; }
    public decimal? FloatShares { get; set; }
    public decimal TotalMarketCap { get; set; }
    public decimal? FloatMarketCap { get; set; }

    public decimal? PeTtm { get; set; }
    public decimal? PeStatic { get; set; }
    public decimal? PeDynamic { get; set; }
    public decimal? Pb { get; set; }
    public decimal? PsTtm { get; set; }
    public decimal? DividendYield { get; set; }

    public bool IsSuspended { get; set; }
    public DateTime? SharesAsOf { get; set; }
    public string QualityFlag { get; set; } = "";
    public string Source { get; set; } = "";
    public string? SourceUrl { get; set; }
    public string? RawHash { get; set; }
    public string FetchedAt { get; set; } = "";
}

public sealed class MarketImportResult
{
    public int Scanned { get; set; }
    public int Rows { get; set; }
    public int Written { get; set; }
    public string? TradeDate { get; set; }
    public List<string> Errors { get; } = [];
}

public sealed class MarketDayStatus
{
    public DateTime TradeDate { get; set; }
    public int RowCount { get; set; }
}

public sealed class MarketStats
{
    public DateTime TradeDate { get; set; }
    public int Companies { get; set; }
    public decimal? TotalMarketCap { get; set; }
    public decimal? FloatMarketCap { get; set; }
    public decimal? MedianMarketCap { get; set; }
}
