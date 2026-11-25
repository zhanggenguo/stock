using Dapper;
using StockAnalysis.Api.Data;
using StockAnalysis.Api.Models;
using StockAnalysis.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<Db>();
builder.Services.AddSingleton<MigrationRunner>();
builder.Services.AddSingleton<CompanyImporter>();
builder.Services.AddSingleton<CompanyQuery>();
builder.Services.AddSingleton<IndustryQuery>();
builder.Services.AddSingleton<MarketImport>();
builder.Services.AddSingleton<MarketQuery>();
builder.Services.AddSingleton<InfoSourceService>();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    using var scope = app.Services.CreateScope();
    var report = await scope.ServiceProvider.GetRequiredService<MigrationRunner>().MigrateAsync();
    if (report.HasError)
        throw new InvalidOperationException("启动时数据库迁移失败：" +
            string.Join("; ", report.Results.Where(x => x.Error is not null).Select(x => x.Error)));
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapGet("/health", async (Db db) =>
{
    try
    {
        await using var conn = db.Open();
        var version = await conn.ExecuteScalarAsync<string>("SELECT VERSION()");
        return Results.Ok(new { status = "ok", mysql = version });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "error", message = ex.Message }, statusCode: 503);
    }
});

app.MapGet("/api/migrate/status", async (MigrationRunner runner) =>
    Results.Ok(await runner.StatusAsync()));

app.MapPost("/api/migrate", async (MigrationRunner runner) =>
{
    var report = await runner.MigrateAsync();
    return report.HasError ? Results.Json(report, statusCode: 500) : Results.Ok(report);
});

// 兼容旧调用：等价执行全部待应用迁移
app.MapPost("/api/company/init", async (MigrationRunner runner) =>
{
    var report = await runner.MigrateAsync();
    return report.HasError ? Results.Json(report, statusCode: 500) : Results.Ok(report);
});

static string RawPath(IConfiguration config)
{
    var rawDir = config["RawData:Directory"] ?? "data/raw/companies";
    return Path.IsPathRooted(rawDir) ? rawDir : Path.Combine(Directory.GetCurrentDirectory(), rawDir);
}

app.MapPost("/api/company/import", async (CompanyImporter importer, MigrationRunner runner, IConfiguration config, bool dryRun = false) =>
{
    if (!dryRun)
    {
        var migration = await runner.MigrateAsync();
        if (migration.HasError)
            return Results.Json(new { message = "数据库迁移失败", migration }, statusCode: 500);
    }
    var result = await importer.ImportAsync(RawPath(config), dryRun: dryRun);
    return Results.Ok(new
    {
        result.Scanned,
        result.Imported,
        errorCount = result.Errors.Count,
        errors = result.Errors.Take(20)
    });
});

app.MapGet("/api/company/preview", async (CompanyImporter importer, IConfiguration config, int take = 3) =>
    Results.Ok(await importer.PreviewAsync(RawPath(config), Math.Clamp(take, 1, 50))));

app.MapGet("/api/company", async (CompanyQuery query, string? keyword, string? exchange, int limit = 50, string? industryId = null, int? level = null) =>
    Results.Ok(await query.SearchAsync(keyword, exchange, limit, industryId, level)));

app.MapGet("/api/company/{companyId}", async (CompanyQuery query, string companyId) =>
{
    var company = await query.GetAsync(companyId);
    return company is null ? Results.NotFound() : Results.Ok(company);
});

app.MapGet("/api/company/stats", async (CompanyQuery query) => Results.Ok(await query.StatsAsync()));

// 行业树：节点带直属数与含后代总数
app.MapGet("/api/industry/tree", async (IndustryQuery query, string scheme = "EM") =>
    Results.Ok(await query.TreeAsync(scheme)));

// 行业映射重建：import 之后调用，幂等
app.MapPost("/api/industry/rebuild", async (IndustryQuery query, string scheme = "EM") =>
{
    try
    {
        return Results.Ok(await query.RebuildAsync(scheme));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

static string MarketRawPath(IConfiguration config)
{
    var rawDir = config["MarketData:Directory"] ?? "data/raw/market";
    return Path.IsPathRooted(rawDir) ? rawDir : Path.Combine(Directory.GetCurrentDirectory(), rawDir);
}

static DateTime? ParseDate(string? text) =>
    DateTime.TryParseExact(text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.None, out var d) ? d : null;

// 第二阶段：日市值采集入库，按 (company_id, trade_date) 幂等
app.MapPost("/api/market/import", async (MarketImport importer, MigrationRunner runner,
    IConfiguration config, string? tradeDate = null) =>
{
    var migration = await runner.MigrateAsync();
    if (migration.HasError)
        return Results.Json(new { message = "数据库迁移失败", migration }, statusCode: 500);
    var result = await importer.ImportAsync(MarketRawPath(config), tradeDate);
    return Results.Ok(new
    {
        result.Scanned,
        result.Rows,
        result.Written,
        result.TradeDate,
        errorCount = result.Errors.Count,
        errors = result.Errors.Take(20)
    });
});

app.MapGet("/api/market/daily", async (MarketQuery query, string? companyId, string? from,
    string? to, int limit = 500) =>
    Results.Ok(await query.DailyAsync(companyId, ParseDate(from), ParseDate(to), limit)));

app.MapGet("/api/market/stats", async (MarketQuery query, string? tradeDate) =>
    Results.Ok(await query.StatsAsync(ParseDate(tradeDate))));

app.MapGet("/api/market/status", async (MarketQuery query) => Results.Ok(await query.StatusAsync()));

// 信息来源目录：完整 CRUD + 文档批量导入。Schema 由 SQL 迁移管理，服务首次使用时自动执行迁移器（幂等）。
app.MapGet("/api/info-source", async (InfoSourceService service, string? category) =>
    Results.Ok(await service.ListAsync(category)));

app.MapPost("/api/info-source/import", async (InfoSourceService service, InfoSourceImportItem[] items) =>
{
    try
    {
        return Results.Ok(await service.ImportAsync(items));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapGet("/api/info-source/{id:long}", async (InfoSourceService service, long id) =>
{
    var item = await service.GetAsync(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});

app.MapPost("/api/info-source", async (InfoSourceService service, InfoSourceInput input) =>
{
    try
    {
        var id = await service.CreateAsync(input);
        return Results.Created($"/api/info-source/{id}", await service.GetAsync(id));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPut("/api/info-source/{id:long}", async (InfoSourceService service, long id, InfoSourceInput input) =>
{
    try
    {
        if (!await service.UpdateAsync(id, input)) return Results.NotFound();
        return Results.Ok(await service.GetAsync(id));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapDelete("/api/info-source/{id:long}", async (InfoSourceService service, long id) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

app.Run();
