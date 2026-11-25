namespace StockAnalysis.Api.Models;

public sealed class Company
{
    public string CompanyId { get; set; } = "";
    public string Code { get; set; } = "";
    public string Exchange { get; set; } = "";
    public string? Board { get; set; }
    public string Name { get; set; } = "";
    public string? NameFull { get; set; }
    public string? NameEn { get; set; }
    public string? FormerNames { get; set; }
    public string? ListDate { get; set; }
    public string? FoundDate { get; set; }
    public string? IndustryEm { get; set; }
    public string? IndustryCsrc { get; set; }
    public string? Province { get; set; }
    public string? LegalPerson { get; set; }
    public string? Chairman { get; set; }
    public string? President { get; set; }
    public string? Secretary { get; set; }
    public string? ActualController { get; set; }
    public decimal? RegCapitalWan { get; set; }
    public int? EmpNum { get; set; }
    public string? Auditor { get; set; }
    public string? LawFirm { get; set; }
    public string? Website { get; set; }
    public string? Tel { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? RegAddress { get; set; }
    public decimal? IssuePrice { get; set; }
    public decimal? ParValue { get; set; }
    public decimal? TotalIssueNum { get; set; }
    public string? Profile { get; set; }
    public string? BusinessScope { get; set; }
    public string? SourceUrl { get; set; }
    public string FetchedAt { get; set; } = "";
    public string? RawHash { get; set; }
}

public sealed class ImportResult
{
    public int Scanned { get; set; }
    public int Imported { get; set; }
    public List<string> Errors { get; } = [];
}

public sealed class FetchLog
{
    public string Task { get; set; } = "";
    public string Source { get; set; } = "";
    public string FetchedAt { get; set; } = "";
    public int Total { get; set; }
    public int Ok { get; set; }
    public int Fail { get; set; }
    public string? Note { get; set; }
}
