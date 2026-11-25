using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace StockQuery.WinForms
{
    /// <summary>
    /// 详情窗口：第一页“历史股价”展示日行情表格与收盘价折线图；
    /// 第二页“公司资料”保留原有 35 个字段。图表/表格控件在 DetailForm.Designer.cs 中，可用设计器调整。
    /// </summary>
    public partial class DetailForm : Form
    {
        private ApiClient _api;

        /// <summary>设计器使用的无参构造函数，不要在此访问 _api。</summary>
        public DetailForm()
        {
            InitializeComponent();
        }

        /// <summary>运行时构造函数：注入 API 与公司档案，再加载历史股价。</summary>
        public DetailForm(ApiClient api, Company c) : this()
        {
            _api = api;
            Text = (c.Name ?? "") + "（" + (c.Code ?? "") + "）";
            FillCompany(c);
            LoadMarket(c.CompanyId);
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FillCompany(Company c)
        {
            int y = 12;
            Add(_panel, "公司 ID", c.CompanyId, ref y);
            Add(_panel, "代码", c.Code, ref y);
            Add(_panel, "交易所", c.Exchange, ref y);
            Add(_panel, "板块", c.Board, ref y);
            Add(_panel, "简称", c.Name, ref y);
            Add(_panel, "全称", c.NameFull, ref y);
            Add(_panel, "英文名", c.NameEn, ref y);
            Add(_panel, "曾用名", c.FormerNames, ref y);
            Add(_panel, "上市日", c.ListDate, ref y);
            Add(_panel, "成立日", c.FoundDate, ref y);
            Add(_panel, "东财行业", c.IndustryEm, ref y);
            Add(_panel, "证监会行业", c.IndustryCsrc, ref y);
            Add(_panel, "省份", c.Province, ref y);
            Add(_panel, "法人", c.LegalPerson, ref y);
            Add(_panel, "董事长", c.Chairman, ref y);
            Add(_panel, "总经理", c.President, ref y);
            Add(_panel, "董秘", c.Secretary, ref y);
            Add(_panel, "实控人", c.ActualController, ref y);
            Add(_panel, "注册资本(万)", Num(c.RegCapitalWan), ref y);
            Add(_panel, "员工数", Num(c.EmpNum), ref y);
            Add(_panel, "审计机构", c.Auditor, ref y);
            Add(_panel, "律所", c.LawFirm, ref y);
            Add(_panel, "网址", c.Website, ref y);
            Add(_panel, "电话", c.Tel, ref y);
            Add(_panel, "邮箱", c.Email, ref y);
            Add(_panel, "办公地址", c.Address, ref y, true);
            Add(_panel, "注册地址", c.RegAddress, ref y, true);
            Add(_panel, "发行价", Num(c.IssuePrice), ref y);
            Add(_panel, "面值", Num(c.ParValue), ref y);
            Add(_panel, "发行数量", Num(c.TotalIssueNum), ref y);
            Add(_panel, "抓取时间", c.FetchedAt, ref y);
            Add(_panel, "原始哈希", c.RawHash, ref y);
            Add(_panel, "来源链接", c.SourceUrl, ref y, true);
            Add(_panel, "简介", c.Profile, ref y, true);
            Add(_panel, "经营范围", c.BusinessScope, ref y, true);
        }

        /// <summary>API 返回按交易日倒序；表格保持倒序，图表反转成时间正序。</summary>
        private void LoadMarket(string companyId)
        {
            if (string.IsNullOrEmpty(companyId)) return;
            try
            {
                List<MarketHistory> rows = _api.GetMarketDaily(companyId, 500);
                if (rows == null || rows.Count == 0)
                {
                    ShowPriceHint("暂无历史股价记录（当前仅回填最近 3 个交易日）");
                    return;
                }

                _gridMarket.DataSource = rows;
                var series = _chart.Series["收盘价"];
                series.Points.Clear();
                for (int i = rows.Count - 1; i >= 0; i--)
                {
                    MarketHistory r = rows[i];
                    if (r.TradeDate == default(DateTime)) continue;
                    series.Points.AddXY(r.TradeDate, Convert.ToDouble(r.Close));
                }
                _tabPrice.Text = "历史股价 (" + rows.Count + ")";
            }
            catch (ApiException ex)
            {
                ShowPriceHint("历史股价加载失败：" + ex.Message);
            }
        }

        private void ShowPriceHint(string message)
        {
            _lblPriceHint.Text = message;
            _lblPriceHint.Visible = true;
        }

        private static string Num(object value)
        {
            return value == null ? "" : Convert.ToString(value);
        }

        private static void Add(Control parent, string label, string value, ref int y, bool multiline = false)
        {
            var lb = new Label
            {
                Text = label,
                Left = 12,
                Top = y + 4,
                Width = 104,
                TextAlign = ContentAlignment.MiddleRight
            };
            var tb = new TextBox
            {
                Left = 124,
                Top = y,
                Width = 600,
                ReadOnly = true,
                Multiline = multiline,
                Height = multiline ? 76 : 24,
                Text = value ?? "",
                BackColor = SystemColors.Window
            };
            parent.Controls.Add(lb);
            parent.Controls.Add(tb);
            y += multiline ? 84 : 30;
        }
    }
}
