using Newtonsoft.Json;

namespace StockQuery.WinForms
{
    /// <summary>公司列表/详情。字段对应 .NET 10 API 的 Company 输出。</summary>
    public sealed class Company
    {
        [JsonProperty("companyId")] public string CompanyId { get; set; }
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("exchange")] public string Exchange { get; set; }
        [JsonProperty("board")] public string Board { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("nameFull")] public string NameFull { get; set; }
        [JsonProperty("nameEn")] public string NameEn { get; set; }
        [JsonProperty("formerNames")] public string FormerNames { get; set; }
        [JsonProperty("listDate")] public string ListDate { get; set; }
        [JsonProperty("foundDate")] public string FoundDate { get; set; }
        [JsonProperty("industryEm")] public string IndustryEm { get; set; }
        [JsonProperty("industryCsrc")] public string IndustryCsrc { get; set; }
        [JsonProperty("province")] public string Province { get; set; }
        [JsonProperty("legalPerson")] public string LegalPerson { get; set; }
        [JsonProperty("chairman")] public string Chairman { get; set; }
        [JsonProperty("president")] public string President { get; set; }
        [JsonProperty("secretary")] public string Secretary { get; set; }
        [JsonProperty("actualController")] public string ActualController { get; set; }
        [JsonProperty("regCapitalWan")] public decimal? RegCapitalWan { get; set; }
        [JsonProperty("empNum")] public int? EmpNum { get; set; }
        [JsonProperty("auditor")] public string Auditor { get; set; }
        [JsonProperty("lawFirm")] public string LawFirm { get; set; }
        [JsonProperty("website")] public string Website { get; set; }
        [JsonProperty("tel")] public string Tel { get; set; }
        [JsonProperty("email")] public string Email { get; set; }
        [JsonProperty("address")] public string Address { get; set; }
        [JsonProperty("regAddress")] public string RegAddress { get; set; }
        [JsonProperty("issuePrice")] public decimal? IssuePrice { get; set; }
        [JsonProperty("parValue")] public decimal? ParValue { get; set; }
        [JsonProperty("totalIssueNum")] public decimal? TotalIssueNum { get; set; }
        [JsonProperty("profile")] public string Profile { get; set; }
        [JsonProperty("businessScope")] public string BusinessScope { get; set; }
        [JsonProperty("sourceUrl")] public string SourceUrl { get; set; }
        [JsonProperty("fetchedAt")] public string FetchedAt { get; set; }
        [JsonProperty("rawHash")] public string RawHash { get; set; }
    }

    /// <summary>日市值/行情记录。字段对应 GET /api/market/daily 的 MarketCap 输出。</summary>
    public sealed class MarketHistory
    {
        [JsonProperty("id")] public long Id { get; set; }
        [JsonProperty("companyId")] public string CompanyId { get; set; }
        [JsonProperty("tradeDate")] public System.DateTime TradeDate { get; set; }
        [JsonProperty("exchange")] public string Exchange { get; set; }
        [JsonProperty("close")] public decimal Close { get; set; }
        [JsonProperty("pctChange")] public decimal? PctChange { get; set; }
        [JsonProperty("amplitude")] public decimal? Amplitude { get; set; }
        [JsonProperty("volume")] public decimal? Volume { get; set; }
        [JsonProperty("amount")] public decimal? Amount { get; set; }
        [JsonProperty("turnoverRate")] public decimal? TurnoverRate { get; set; }
        [JsonProperty("totalShares")] public decimal? TotalShares { get; set; }
        [JsonProperty("floatShares")] public decimal? FloatShares { get; set; }
        [JsonProperty("totalMarketCap")] public decimal TotalMarketCap { get; set; }
        [JsonProperty("floatMarketCap")] public decimal? FloatMarketCap { get; set; }
        [JsonProperty("peTtm")] public decimal? PeTtm { get; set; }
        [JsonProperty("peStatic")] public decimal? PeStatic { get; set; }
        [JsonProperty("peDynamic")] public decimal? PeDynamic { get; set; }
        [JsonProperty("pb")] public decimal? Pb { get; set; }
        [JsonProperty("psTtm")] public decimal? PsTtm { get; set; }
        [JsonProperty("dividendYield")] public decimal? DividendYield { get; set; }
        [JsonProperty("isSuspended")] public bool IsSuspended { get; set; }
        [JsonProperty("sharesAsOf")] public System.DateTime? SharesAsOf { get; set; }
        [JsonProperty("qualityFlag")] public string QualityFlag { get; set; }
        [JsonProperty("source")] public string Source { get; set; }
        [JsonProperty("sourceUrl")] public string SourceUrl { get; set; }
        [JsonProperty("rawHash")] public string RawHash { get; set; }
        [JsonProperty("fetchedAt")] public string FetchedAt { get; set; }

        // 服务端市值单位是“元”，表格按“亿元”展示；只读计算属性，不参与反序列化。
        public decimal? TotalMarketCapYi { get { return TotalMarketCap / 100000000m; } }
        public decimal? FloatMarketCapYi { get { return FloatMarketCap / 100000000m; } }
    }

    /// <summary>信息来源目录条目，对应 POST/PUT /api/info-source 的输入输出。</summary>
    public sealed class InfoSource
    {
        [JsonProperty("id")] public long Id { get; set; }
        [JsonProperty("category")] public string Category { get; set; }
        [JsonProperty("entryType")] public string EntryType { get; set; }
        [JsonProperty("siteName")] public string SiteName { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("keyContent")] public string KeyContent { get; set; }
        [JsonProperty("url")] public string Url { get; set; }
        [JsonProperty("sortOrder")] public int SortOrder { get; set; }
        [JsonProperty("createdAt")] public string CreatedAt { get; set; }
        [JsonProperty("updatedAt")] public string UpdatedAt { get; set; }
    }

    public sealed class Stats
    {
        [JsonProperty("total")] public int Total { get; set; }
        [JsonProperty("latestFetchedAt")] public string LatestFetchedAt { get; set; }
        [JsonProperty("byExchange")] public System.Collections.Generic.List<ExchangeCount> ByExchange { get; set; }
    }

    public sealed class ExchangeCount
    {
        [JsonProperty("exchange")] public string Exchange { get; set; }
        [JsonProperty("count")] public int Count { get; set; }
    }

    /// <summary>行业树节点。TotalCount 含自身及全部后代公司数。</summary>
    public sealed class IndustryNode
    {
        [JsonProperty("industryId")] public string IndustryId { get; set; }
        [JsonProperty("scheme")] public string Scheme { get; set; }
        [JsonProperty("level")] public int Level { get; set; }
        [JsonProperty("parentId")] public string ParentId { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("directCount")] public int DirectCount { get; set; }
        [JsonProperty("totalCount")] public int TotalCount { get; set; }
    }

    public sealed class Health
    {
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("mysql")] public string Mysql { get; set; }
    }

    public sealed class MigrationInfo
    {
        [JsonProperty("version")] public int Version { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("checksum")] public string Checksum { get; set; }
        [JsonProperty("state")] public string State { get; set; }
        [JsonProperty("appliedAt")] public string AppliedAt { get; set; }
    }
}
