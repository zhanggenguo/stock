using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace StockQuery.WinForms
{
    /// <summary>
    /// 基础数据查询主界面：全部数据来自 API，不含任何 SQL。
    /// 界面布局在 MainForm.Designer.cs 的 InitializeComponent() 中，可直接用 VS 设计器调整。
    /// </summary>
    public partial class MainForm : Form
    {
        private ApiClient _api;
        private string _selectedIndustryId = null;
        private string _selectedIndustryName = "全部公司";
        private string _statsText = "";
        private bool _suppressTreeSelection;

        /// <summary>设计器使用的无参构造函数，不要在此访问 _api。</summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>运行时构造函数：注入 API 客户端后再挂业务事件。</summary>
        public MainForm(ApiClient api) : this()
        {
            _api = api;
            _hint.Text = "双击行或右键 → 详情    |    数据源：" + _api.BaseUrl;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (_api == null) return; // 设计器预览时不请求数据
            try
            {
                var health = _api.CheckHealth();
                if (!string.Equals(health.Status, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("API 服务状态异常：" + health.Status, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                LoadIndustryTree();
                RefreshStats();
                DoSearch();
                PinTreeTop(); // 所有布局完成后再固定一次，消除 DPI 缩放带来的半行偏移
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "无法加载数据", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusText.Text = "API 未启动：" + _api.BaseUrl;
            }
        }

        private void LoadIndustryTree()
        {
            // 初始化期间屏蔽 AfterSelect：避免 ExpandAll/SelectedNode 触发查询与重入布局
            _suppressTreeSelection = true;
            try
            {
                _tree.Nodes.Clear();
                var root = new TreeNode("全部公司 (5108)");
                root.Tag = null;
                _tree.Nodes.Add(root);

                var nodes = _api.GetIndustryTree("EM");
                var byId = new Dictionary<string, TreeNode>();
                foreach (var n in nodes)
                    byId[n.IndustryId] = new TreeNode(n.Name + " (" + n.TotalCount + ")") { Tag = n.IndustryId };

                foreach (var n in nodes)
                {
                    TreeNode parent = string.IsNullOrEmpty(n.ParentId) ? root : (byId.ContainsKey(n.ParentId) ? byId[n.ParentId] : root);
                    parent.Nodes.Add(byId[n.IndustryId]);
                }
                _tree.ExpandAll();
                _tree.SelectedNode = root;
                PinTreeTop();
            }
            finally
            {
                _suppressTreeSelection = false;
            }
        }

        /// <summary>把树滚动位置固定到首节点，防止高 DPI 下首行被截掉上半部分。</summary>
        private void PinTreeTop()
        {
            if (_tree.Nodes.Count > 0)
                _tree.TopNode = _tree.Nodes[0];
        }

        private void Tree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_suppressTreeSelection) return;
            _selectedIndustryId = e.Node.Tag as string;
            _selectedIndustryName = string.IsNullOrEmpty(_selectedIndustryId) ? "全部公司" : e.Node.Text;
            try { DoSearch(); }
            catch (ApiException ex) { ShowError(ex); }
        }

        private void Reset_Click(object sender, EventArgs e)
        {
            _selectedIndustryId = null;
            _selectedIndustryName = "全部公司";
            if (_tree.Nodes.Count > 0)
                _tree.SelectedNode = _tree.Nodes[0];
            try { DoSearch(); }
            catch (ApiException ex) { ShowError(ex); }
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            try { RefreshStats(); }
            catch (ApiException ex) { ShowError(ex); }
        }

        private void Search_Click(object sender, EventArgs e)
        {
            try { DoSearch(); }
            catch (ApiException ex) { ShowError(ex); }
        }

        private void InfoSources_Click(object sender, EventArgs e)
        {
            try
            {
                using (var form = new InfoSourceForm(_api))
                    form.ShowDialog(this);
            }
            catch (ApiException ex) { ShowError(ex); }
        }

        private void Versions_Click(object sender, EventArgs e)
        {
            try
            {
                var list = _api.GetMigrations();
                var sb = new StringBuilder();
                sb.AppendLine("数据库迁移版本（来自 API）：");
                foreach (var m in list)
                    sb.AppendLine(string.Format("  V{0:D3} {1}  [{2}]  {3}", m.Version, m.Name, m.State, m.AppliedAt));
                MessageBox.Show(sb.ToString(), "迁移版本", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (ApiException ex) { ShowError(ex); }
        }

        private void RefreshStats()
        {
            Stats stats = _api.GetStats();
            var sb = new StringBuilder();
            sb.Append("库内 ").Append(stats.Total).Append(" 家");
            if (stats.ByExchange != null)
                foreach (var x in stats.ByExchange)
                    sb.Append(" | ").Append(x.Exchange).Append(" ").Append(x.Count);
            sb.Append(" | 最近采集 ").Append(stats.LatestFetchedAt);
            _statsText = sb.ToString();
            _statusText.Text = _statsText;
        }

        private void DoSearch()
        {
            string exchange = _exchange.SelectedIndex <= 0 ? "" : _exchange.Text;
            List<Company> rows = _api.Search(_keyword.Text.Trim(), exchange, (int)_limit.Value, _selectedIndustryId);
            _grid.DataSource = rows;
            _hint.Visible = rows.Count == 0;
            _hint.Text = rows.Count == 0 ? "没有匹配的数据" : "双击行或右键 → 详情    |    数据源：" + _api.BaseUrl;
            _statusText.Text = "行业：" + _selectedIndustryName + " | 本次 " + rows.Count + " 条 | " + _statsText;
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OpenDetail(e.RowIndex);
        }

        /// <summary>右键时先把光标落到被点中的行，避免菜单作用到上一行。</summary>
        private void Grid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
            int column = e.ColumnIndex >= 0 ? e.ColumnIndex : FirstVisibleColumn();
            if (column < 0) return;
            _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[column];
        }

        private void GridMenuDetail_Click(object sender, EventArgs e)
        {
            if (_grid.CurrentRow == null) return;
            OpenDetail(_grid.CurrentRow.Index);
        }

        private int FirstVisibleColumn()
        {
            for (int i = 0; i < _grid.Columns.Count; i++)
                if (_grid.Columns[i].Visible) return i;
            return -1;
        }

        /// <summary>双击与右键“详情”共用：取公司档案，再打开带历史股价 Tab 的详情窗口。</summary>
        private void OpenDetail(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _grid.Rows.Count) return;
            string id = Convert.ToString(_grid.Rows[rowIndex].Cells["companyId"].Value);
            if (string.IsNullOrEmpty(id)) return;
            try
            {
                Company detail = _api.GetCompany(id);
                using (var form = new DetailForm(_api, detail))
                    form.ShowDialog(this);
            }
            catch (ApiException ex) { ShowError(ex); }
        }

        private static void ShowError(ApiException ex)
        {
            MessageBox.Show(ex.Message, "请求失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
