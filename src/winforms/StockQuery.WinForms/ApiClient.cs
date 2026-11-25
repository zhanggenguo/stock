using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StockQuery.WinForms
{
    public sealed class ApiException : Exception
    {
        public ApiException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>客户端配置：只保存 API 地址，不含任何数据库凭据。</summary>
    public static class ClientConfig
    {
        private const string FileName = "appsettings.json";

        public static string LoadApiBaseUrl()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
            if (!File.Exists(path))
                throw new ApiException("缺少配置文件：" + path, null);
            try
            {
                var json = JObject.Parse(File.ReadAllText(path));
                var url = (string)json["ApiBaseUrl"];
                if (string.IsNullOrWhiteSpace(url))
                    throw new ApiException("配置项 ApiBaseUrl 为空：" + path, null);
                return url;
            }
            catch (JsonException ex)
            {
                throw new ApiException("配置文件格式错误：" + path, ex);
            }
        }
    }

    /// <summary>
    /// 只通过 HTTP 调用 .NET 10 API。按 ADR-001，客户端不连接 MySQL。
    /// </summary>
    public sealed class ApiClient
    {
        private readonly string _baseUrl;
        private readonly HttpClient _http;

        public ApiClient(string baseUrl)
        {
            _baseUrl = (baseUrl ?? "").TrimEnd('/');
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        public string BaseUrl { get { return _baseUrl; } }

        public Health CheckHealth()
        {
            return Get<Health>("/health");
        }

        public List<Company> Search(string keyword, string exchange, int limit)
        {
            return Search(keyword, exchange, limit, null);
        }

        public List<Company> Search(string keyword, string exchange, int limit, string industryId)
        {
            string url = "/api/company?keyword=" + Uri.EscapeDataString(keyword ?? "")
                + "&exchange=" + Uri.EscapeDataString(exchange ?? "")
                + "&limit=" + limit;
            if (!string.IsNullOrEmpty(industryId))
                url += "&industryId=" + Uri.EscapeDataString(industryId);
            return Get<List<Company>>(url);
        }

        public List<IndustryNode> GetIndustryTree(string scheme)
        {
            return Get<List<IndustryNode>>("/api/industry/tree?scheme=" + Uri.EscapeDataString(scheme ?? "EM"));
        }

        public Company GetCompany(string companyId)
        {
            return Get<Company>("/api/company/" + Uri.EscapeDataString(companyId ?? ""));
        }

        /// <summary>历史日行情，API 按交易日倒序返回；limit 默认取最近 500 条。</summary>
        public List<MarketHistory> GetMarketDaily(string companyId, int limit = 500)
        {
            string url = "/api/market/daily?companyId=" + Uri.EscapeDataString(companyId ?? "")
                + "&limit=" + limit;
            return Get<List<MarketHistory>>(url);
        }

        // ---- 信息来源目录 CRUD ----

        public System.Collections.Generic.List<InfoSource> GetInfoSources()
        {
            return Get<System.Collections.Generic.List<InfoSource>>("/api/info-source");
        }

        public InfoSource CreateInfoSource(InfoSource item)
        {
            var body = new { siteName = item.SiteName, description = item.Description, url = item.Url };
            return Send<InfoSource>("POST", "/api/info-source", body);
        }

        public InfoSource UpdateInfoSource(long id, InfoSource item)
        {
            var body = new { siteName = item.SiteName, description = item.Description, url = item.Url };
            return Send<InfoSource>("PUT", "/api/info-source/" + id, body);
        }

        public void DeleteInfoSource(long id)
        {
            Send<object>("DELETE", "/api/info-source/" + id, null);
        }

        public Stats GetStats()
        {
            return Get<Stats>("/api/company/stats");
        }

        public List<MigrationInfo> GetMigrations()
        {
            return Get<List<MigrationInfo>>("/api/migrate/status");
        }

        /// <summary>发送带 JSON 体的 POST/PUT/DELETE；204 空响应返回默认值。</summary>
        private T Send<T>(string method, string path, object body)
        {
            string url = _baseUrl + path;
            try
            {
                var content = body == null
                    ? null
                    : new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                using (var request = new HttpRequestMessage(new HttpMethod(method), url))
                {
                    if (content != null) request.Content = content;
                    using (var response = _http.SendAsync(request).GetAwaiter().GetResult())
                    {
                        string text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (!response.IsSuccessStatusCode)
                        {
                            string detail = text;
                            try
                            {
                                var obj = JObject.Parse(text);
                                detail = (string)obj["message"] ?? (string)obj["title"] ?? text;
                            }
                            catch (JsonException) { }
                            throw new ApiException("API 请求失败（HTTP " + (int)response.StatusCode + "）：" + detail, null);
                        }
                        if (string.IsNullOrWhiteSpace(text)) return default(T);
                        return JsonConvert.DeserializeObject<T>(text);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("无法连接 API 服务：" + url, ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new ApiException("API 请求超时：" + url, ex);
            }
            catch (JsonException ex)
            {
                throw new ApiException("API 返回内容无法解析：" + url, ex);
            }
        }

        private T Get<T>(string path)
        {
            string url = _baseUrl + path;
            try
            {
                string json = _http.GetStringAsync(url).GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException("无法连接 API 服务：" + url + Environment.NewLine
                    + "请先启动 .NET 10 服务（见 docs/winforms/03-复现手册.md）。", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new ApiException("API 请求超时：" + url, ex);
            }
            catch (JsonException ex)
            {
                throw new ApiException("API 返回内容无法解析，请检查接口地址：" + url, ex);
            }
        }
    }
}
