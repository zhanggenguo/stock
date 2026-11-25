namespace StockAnalysis.Api.Models;

/// <summary>
/// 行业树节点。TotalCount 含自身及全部后代的公司数。
/// </summary>
public sealed class IndustryNode
{
    public string IndustryId { get; set; } = "";
    public string Scheme { get; set; } = "";
    public int Level { get; set; }
    public string? ParentId { get; set; }
    public string Name { get; set; } = "";
    public int DirectCount { get; set; }
    public int TotalCount { get; set; }
}

public sealed class IndustryRebuildResult
{
    public string Scheme { get; set; } = "";
    public int Industries { get; set; }
    public int Mappings { get; set; }
    public int ElapsedMs { get; set; }
}
