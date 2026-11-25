namespace StockAnalysis.Api.Models;

/// <summary>信息来源目录条目，对应 info_source 表。entry_type：site/combo/template/note。</summary>
public sealed class InfoSource
{
    public long Id { get; set; }
    public string Category { get; set; } = "";
    public string EntryType { get; set; } = "site";
    public string SiteName { get; set; } = "";
    public string Description { get; set; } = "";
    public string? KeyContent { get; set; }
    public string? Url { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>新增/更新信息来源的输入；服务端做去空格与必填校验。entryType 缺省为 site。</summary>
public sealed class InfoSourceInput
{
    public string? Category { get; set; }
    public string? EntryType { get; set; }
    public string? SiteName { get; set; }
    public string? Description { get; set; }
    public string? KeyContent { get; set; }
    public string? Url { get; set; }
}

/// <summary>文档批量导入的单条条目（data/raw/info_source.json 的 entries 形状）。</summary>
public sealed class InfoSourceImportItem
{
    public string? Category { get; set; }
    public string? EntryType { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
    public string? KeyContent { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>批量导入结果统计。</summary>
public sealed class InfoSourceImportResult
{
    public int Received { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Total { get; set; }
}
