using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace StockQuery.WinForms
{
    /// <summary>
    /// 信息来源目录：分类筛选 + 列表 + 新增/更新/删除。全部走 API，不直连数据库。
    /// 条目类型与 API 值的映射：网站=site、组合=combo、模板=template、笔记=note。
    /// 控件在 InfoSourceForm.Designer.cs 中，可在 VS 设计器直接调整。
    /// </summary>
    public partial class InfoSourceForm : Form
    {
        /// <summary>界面类型显示名 → API entryType 值。</summary>
        private static readonly string[] TypeNames = { "网站", "组合", "模板", "笔记" };
        private static readonly string[] TypeValues = { "site", "combo", "template", "note" };

        private ApiClient _api;
        private long _selectedId;
        private List<InfoSource> _allRows = new List<InfoSource>();

        /// <summary>设计器使用的无参构造函数。</summary>
        public InfoSourceForm()
        {
            InitializeComponent();
        }

        /// <summary>运行时构造函数：注入 API 客户端。</summary>
        public InfoSourceForm(ApiClient api) : this()
        {
            _api = api;
        }

        private void InfoSourceForm_Load(object sender, EventArgs e)
        {
            if (_api == null) return; // 设计器预览时不请求数据
            _cboEntryType.SelectedIndex = 0;
            try
            {
                LoadList();
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "加载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _hint.Text = "加载失败：" + ex.Message;
            }
        }

        private void LoadList()
        {
            _allRows = _api.GetInfoSources();
            RebuildFilterItems();
            ApplyFilter();
        }

        private void RebuildFilterItems()
        {
            string selected = _cboFilterCategory.SelectedIndex > 0
                ? Convert.ToString(_cboFilterCategory.SelectedItem) : null;
            var categories = _allRows.Select(r => r.Category ?? "")
                .Where(c => c.Length > 0).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList();
            _cboFilterCategory.Items.Clear();
            _cboFilterCategory.Items.Add("全部分类");
            _cboFilterCategory.Items.AddRange(categories.Cast<object>().ToArray());
            int index = selected == null ? 0 : _cboFilterCategory.Items.IndexOf(selected);
            _cboFilterCategory.SelectedIndex = index >= 0 ? index : 0;
        }

        private void ApplyFilter()
        {
            bool all = _cboFilterCategory.SelectedIndex <= 0;
            var rows = all
                ? _allRows
                : _allRows.Where(r => (r.Category ?? "") == Convert.ToString(_cboFilterCategory.SelectedItem)).ToList();
            _grid.DataSource = rows;
            ClearEdit();
            _hint.Text = "共 " + _allRows.Count + " 条，当前显示 " + rows.Count
                + " 条。选择一行可修改或删除；点“新增”后填写信息并保存。";
        }

        private void FilterCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_api == null) return;
            ApplyFilter();
        }

        private static string TypeValueOfIndex(int index) => TypeValues[index];
        private static int TypeIndexOfValue(string value)
        {
            for (int i = 0; i < TypeValues.Length; i++)
                if (TypeValues[i] == value) return i;
            return 0;
        }

        private void ClearEdit()
        {
            _selectedId = 0;
            _txtCategory.Text = "";
            _cboEntryType.SelectedIndex = 0;
            _txtName.Text = "";
            _txtDesc.Text = "";
            _txtKey.Text = "";
            _txtUrl.Text = "";
            _btnDelete.Enabled = false;
            _btnSave.Text = "保存";
        }

        private void FillFromSelection()
        {
            if (_grid.CurrentRow == null) return;
            var item = _grid.CurrentRow.DataBoundItem as InfoSource;
            if (item == null) return;
            _selectedId = item.Id;
            _txtCategory.Text = item.Category ?? "";
            _cboEntryType.SelectedIndex = TypeIndexOfValue(item.EntryType);
            _txtName.Text = item.SiteName ?? "";
            _txtDesc.Text = item.Description ?? "";
            _txtKey.Text = item.KeyContent ?? "";
            _txtUrl.Text = item.Url ?? "";
            _btnDelete.Enabled = true;
            _btnSave.Text = "保存修改";
            _hint.Text = "编辑模式：正在修改 ID " + item.Id + "，点“取消编辑”可还原。";
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            FillFromSelection();
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            _grid.CurrentCell = _grid.Rows[e.RowIndex].Cells[1];
            FillFromSelection();
        }

        private void Add_Click(object sender, EventArgs e)
        {
            ClearEdit();
            _btnSave.Text = "保存新增";
            _hint.Text = "新增模式：填写分类、类型、名称等信息后点“保存新增”。网站类型必须填写网页地址。";
            _txtCategory.Focus();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            if (_selectedId > 0) FillFromSelection();
            else ClearEdit();
            _hint.Text = "已取消编辑。";
        }

        private void Save_Click(object sender, EventArgs e)
        {
            var item = new InfoSource
            {
                Id = _selectedId,
                Category = _txtCategory.Text.Trim(),
                EntryType = TypeValueOfIndex(_cboEntryType.SelectedIndex),
                SiteName = _txtName.Text.Trim(),
                Description = _txtDesc.Text.Trim(),
                KeyContent = _txtKey.Text.Trim(),
                Url = _txtUrl.Text.Trim()
            };
            try
            {
                string message;
                if (_selectedId == 0)
                    message = "已新增：" + _api.CreateInfoSource(item).SiteName;
                else
                    message = "已更新：" + _api.UpdateInfoSource(_selectedId, item).SiteName;
                LoadList();
                _hint.Text = message;
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            if (_selectedId == 0)
            {
                MessageBox.Show("请先选择一行。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("确认删除选中的信息来源？", "删除确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                _api.DeleteInfoSource(_selectedId);
                LoadList();
                _hint.Text = "已删除 ID " + _selectedId + "。";
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Close_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
