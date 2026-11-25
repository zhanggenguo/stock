using System.Text.Json;
using Dapper;
using StockAnalysis.Api.Data;
using StockAnalysis.Api.Models;

namespace StockAnalysis.Api.Services;

public sealed class CompanyImporter(Db db, ILogger<CompanyImporter> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ImportResult> ImportAsync(string rawDirectory, CancellationToken ct = default,
        bool dryRun = false)
    {
        var result = new ImportResult();
        if (!Directory.Exists(rawDirectory))
            throw new DirectoryNotFoundException($"原始目录不存在：{rawDirectory}");

        foreach (var file in Directory.EnumerateFiles(rawDirectory, "*.json").OrderBy(x => x))
        {
            ct.ThrowIfCancellationRequested();
            result.Scanned++;
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                using var doc = JsonDocument.Parse(json);
                var company = Map(doc.RootElement);
                if (!dryRun) await UpsertAsync(company);
                result.Imported++;
            }
            catch (Exception ex)
            {
                var msg = $"{Path.GetFileName(file)}: {ex.Message}";
                result.Errors.Add(msg);
                logger.LogWarning(ex, "解析失败 {File}", file);
            }
        }

        if (!dryRun) await ImportFetchLogAsync(rawDirectory, ct);
        return result;
    }

    public async Task<IReadOnlyList<Company>> PreviewAsync(string rawDirectory, int take,
        CancellationToken ct = default)
    {
        var list = new List<Company>();
        foreach (var file in Directory.EnumerateFiles(rawDirectory, "*.json").OrderBy(x => x).Take(take))
        {
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(file, ct));
            list.Add(Map(doc.RootElement));
        }
        return list;
    }

    private static Company Map(JsonElement root)
    {
        var payload = root.GetProperty("payload");
        var survey = payload.GetProperty("survey");
        var holder = payload.GetProperty("holder");
        var jb = survey.GetProperty("jbzl")[0];
        var fx = survey.TryGetProperty("fxxg", out var fxxg) && fxxg.GetArrayLength() > 0
            ? fxxg[0] : default;
        var sj = holder.TryGetProperty("sjkzr", out var sjkzr) && sjkzr.GetArrayLength() > 0
            ? sjkzr[0] : default;

        static string? S(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() : null;

        static decimal? D(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
                ? v.GetDecimal() : null;

        static int? I(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
                ? v.GetInt32() : null;

        var urls = root.TryGetProperty("source_urls", out var u) && u.ValueKind == JsonValueKind.Array
            ? string.Join(" | ", u.EnumerateArray().Select(x => x.GetString()))
            : null;

        return new Company
        {
            CompanyId = S(root, "company_id") ?? "",
            Code = S(root, "code") ?? "",
            Exchange = S(root, "exchange") ?? "",
            FetchedAt = S(root, "fetched_at") ?? "",
            RawHash = S(root, "raw_hash"),
            SourceUrl = urls,
            Board = S(jb, "SECURITY_TYPE"),
            Name = S(jb, "SECURITY_NAME_ABBR") ?? "",
            NameFull = S(jb, "ORG_NAME"),
            NameEn = S(jb, "ORG_NAME_EN"),
            FormerNames = S(jb, "FORMERNAME"),
            IndustryEm = S(jb, "EM2016"),
            IndustryCsrc = S(jb, "INDUSTRYCSRC1"),
            Province = S(jb, "PROVINCE"),
            LegalPerson = S(jb, "LEGAL_PERSON"),
            Chairman = S(jb, "CHAIRMAN"),
            President = S(jb, "PRESIDENT"),
            Secretary = S(jb, "SECRETARY"),
            ActualController = S(sj, "HOLDER_NAME"),
            RegCapitalWan = D(jb, "REG_CAPITAL"),
            EmpNum = I(jb, "EMP_NUM"),
            Auditor = S(jb, "ACCOUNTFIRM_NAME"),
            LawFirm = S(jb, "LAW_FIRM"),
            Website = S(jb, "ORG_WEB"),
            Tel = S(jb, "ORG_TEL"),
            Email = S(jb, "ORG_EMAIL"),
            Address = S(jb, "ADDRESS"),
            RegAddress = S(jb, "REG_ADDRESS"),
            Profile = S(jb, "ORG_PROFILE"),
            BusinessScope = S(jb, "BUSINESS_SCOPE"),
            ListDate = DatePart(S(fx, "LISTING_DATE")),
            FoundDate = DatePart(S(fx, "FOUND_DATE")),
            IssuePrice = D(fx, "ISSUE_PRICE"),
            ParValue = D(fx, "PAR_VALUE"),
            TotalIssueNum = D(fx, "TOTAL_ISSUE_NUM"),
        };
    }

    private static string? DatePart(string? value) =>
        value is { Length: >= 10 } ? value[..10] : value;

    private async Task UpsertAsync(Company c)
    {
        const string sql = """
            INSERT INTO company (
              company_id, code, exchange, board, name, name_full, name_en, former_names,
              list_date, found_date, industry_em, industry_csrc, province, legal_person,
              chairman, president, secretary, actual_controller, reg_capital_wan, emp_num,
              auditor, law_firm, website, tel, email, address, reg_address, issue_price,
              par_value, total_issue_num, profile, business_scope, source_url, fetched_at, raw_hash
            ) VALUES (
              @CompanyId, @Code, @Exchange, @Board, @Name, @NameFull, @NameEn, @FormerNames,
              @ListDate, @FoundDate, @IndustryEm, @IndustryCsrc, @Province, @LegalPerson,
              @Chairman, @President, @Secretary, @ActualController, @RegCapitalWan, @EmpNum,
              @Auditor, @LawFirm, @Website, @Tel, @Email, @Address, @RegAddress, @IssuePrice,
              @ParValue, @TotalIssueNum, @Profile, @BusinessScope, @SourceUrl, @FetchedAt, @RawHash
            )
            ON DUPLICATE KEY UPDATE
              code=VALUES(code), exchange=VALUES(exchange), board=VALUES(board), name=VALUES(name),
              name_full=VALUES(name_full), name_en=VALUES(name_en), former_names=VALUES(former_names),
              list_date=VALUES(list_date), found_date=VALUES(found_date), industry_em=VALUES(industry_em),
              industry_csrc=VALUES(industry_csrc), province=VALUES(province), legal_person=VALUES(legal_person),
              chairman=VALUES(chairman), president=VALUES(president), secretary=VALUES(secretary),
              actual_controller=VALUES(actual_controller), reg_capital_wan=VALUES(reg_capital_wan),
              emp_num=VALUES(emp_num), auditor=VALUES(auditor), law_firm=VALUES(law_firm),
              website=VALUES(website), tel=VALUES(tel), email=VALUES(email), address=VALUES(address),
              reg_address=VALUES(reg_address), issue_price=VALUES(issue_price), par_value=VALUES(par_value),
              total_issue_num=VALUES(total_issue_num), profile=VALUES(profile),
              business_scope=VALUES(business_scope), source_url=VALUES(source_url),
              fetched_at=VALUES(fetched_at), raw_hash=VALUES(raw_hash)
            """;
        await using var conn = db.Open();
        await conn.ExecuteAsync(sql, c);
    }

    private async Task ImportFetchLogAsync(string rawDirectory, CancellationToken ct)
    {
        var path = Path.Combine(Path.GetDirectoryName(rawDirectory)!, "fetch_log.json");
        if (!File.Exists(path)) return;
        try
        {
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
            var r = doc.RootElement;
            var log = new FetchLog
            {
                Task = "company_info",
                Source = "eastmoney",
                FetchedAt = r.TryGetProperty("fetched_at", out var t) ? t.GetString() ?? "" : "",
                Total = r.TryGetProperty("total", out var total) ? total.GetInt32() : 0,
                Ok = r.TryGetProperty("ok", out var ok) ? ok.GetInt32() : 0,
                Fail = r.TryGetProperty("fail", out var fail) ? fail.GetInt32() : 0,
                Note = r.TryGetProperty("failures", out var f) && f.GetArrayLength() > 0
                    ? f[0].GetRawText() : null,
            };
            await using var conn = db.Open();
            await conn.ExecuteAsync("""
                INSERT INTO fetch_log(task,source,fetched_at,total,ok,fail,note)
                VALUES (@Task,@Source,@FetchedAt,@Total,@Ok,@Fail,@Note)
                """, log);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "导入 fetch_log 失败");
        }
    }
}
