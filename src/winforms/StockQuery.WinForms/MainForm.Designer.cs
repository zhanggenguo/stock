namespace StockQuery.WinForms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法。请勿用代码编辑器修改此方法的内容。
        /// 所有界面调整请直接在 VS 设计器中拖动控件完成，保存后设计器会重写本方法。
        /// </summary>
        private void InitializeComponent()
        {
            this._topPanel = new System.Windows.Forms.Panel();
            this._btnInfoSources = new System.Windows.Forms.Button();
            this._btnVersions = new System.Windows.Forms.Button();
            this._btnRefresh = new System.Windows.Forms.Button();
            this._btnReset = new System.Windows.Forms.Button();
            this._btnSearch = new System.Windows.Forms.Button();
            this._limit = new System.Windows.Forms.NumericUpDown();
            this._lblLimit = new System.Windows.Forms.Label();
            this._exchange = new System.Windows.Forms.ComboBox();
            this._lblExchange = new System.Windows.Forms.Label();
            this._keyword = new System.Windows.Forms.TextBox();
            this._lblKeyword = new System.Windows.Forms.Label();
            this._treePanel = new System.Windows.Forms.Panel();
            this._tree = new System.Windows.Forms.TreeView();
            this._lblTreeTitle = new System.Windows.Forms.Label();
            this._splitter = new System.Windows.Forms.Splitter();
            this._body = new System.Windows.Forms.Panel();
            this._grid = new System.Windows.Forms.DataGridView();
            this._gridMenu = new System.Windows.Forms.ContextMenuStrip();
            this._menuDetail = new System.Windows.Forms.ToolStripMenuItem();
            this._hint = new System.Windows.Forms.Label();
            this._status = new System.Windows.Forms.StatusStrip();
            this._statusText = new System.Windows.Forms.ToolStripStatusLabel();
            this._topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._limit)).BeginInit();
            this._treePanel.SuspendLayout();
            this._body.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this._status.SuspendLayout();
            this.SuspendLayout();
            // 
            // _topPanel
            // 
            this._topPanel.Controls.Add(this._btnInfoSources);
            this._topPanel.Controls.Add(this._btnVersions);
            this._topPanel.Controls.Add(this._btnRefresh);
            this._topPanel.Controls.Add(this._btnReset);
            this._topPanel.Controls.Add(this._btnSearch);
            this._topPanel.Controls.Add(this._limit);
            this._topPanel.Controls.Add(this._lblLimit);
            this._topPanel.Controls.Add(this._exchange);
            this._topPanel.Controls.Add(this._lblExchange);
            this._topPanel.Controls.Add(this._keyword);
            this._topPanel.Controls.Add(this._lblKeyword);
            this._topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._topPanel.Location = new System.Drawing.Point(0, 0);
            this._topPanel.Name = "_topPanel";
            this._topPanel.Padding = new System.Windows.Forms.Padding(10, 10, 10, 6);
            this._topPanel.Size = new System.Drawing.Size(1180, 52);
            this._topPanel.TabIndex = 2;
            // 
            // _btnInfoSources
            // 
            this._btnInfoSources.Location = new System.Drawing.Point(1064, 10);
            this._btnInfoSources.Name = "_btnInfoSources";
            this._btnInfoSources.Size = new System.Drawing.Size(104, 28);
            this._btnInfoSources.TabIndex = 10;
            this._btnInfoSources.Text = "信息来源";
            this._btnInfoSources.UseVisualStyleBackColor = true;
            this._btnInfoSources.Click += new System.EventHandler(this.InfoSources_Click);
            // 
            // _btnVersions
            // 
            this._btnVersions.Location = new System.Drawing.Point(834, 10);
            this._btnVersions.Name = "_btnVersions";
            this._btnVersions.Size = new System.Drawing.Size(84, 28);
            this._btnVersions.TabIndex = 9;
            this._btnVersions.Text = "迁移版本";
            this._btnVersions.UseVisualStyleBackColor = true;
            this._btnVersions.Click += new System.EventHandler(this.Versions_Click);
            // 
            // _btnRefresh
            // 
            this._btnRefresh.Location = new System.Drawing.Point(742, 10);
            this._btnRefresh.Name = "_btnRefresh";
            this._btnRefresh.Size = new System.Drawing.Size(84, 28);
            this._btnRefresh.TabIndex = 8;
            this._btnRefresh.Text = "刷新统计";
            this._btnRefresh.UseVisualStyleBackColor = true;
            this._btnRefresh.Click += new System.EventHandler(this.Refresh_Click);
            // 
            // _btnReset
            // 
            this._btnReset.Location = new System.Drawing.Point(652, 10);
            this._btnReset.Name = "_btnReset";
            this._btnReset.Size = new System.Drawing.Size(82, 28);
            this._btnReset.TabIndex = 7;
            this._btnReset.Text = "全部公司";
            this._btnReset.UseVisualStyleBackColor = true;
            this._btnReset.Click += new System.EventHandler(this.Reset_Click);
            // 
            // _btnSearch
            // 
            this._btnSearch.Location = new System.Drawing.Point(570, 10);
            this._btnSearch.Name = "_btnSearch";
            this._btnSearch.Size = new System.Drawing.Size(74, 28);
            this._btnSearch.TabIndex = 6;
            this._btnSearch.Text = "查询";
            this._btnSearch.UseVisualStyleBackColor = true;
            this._btnSearch.Click += new System.EventHandler(this.Search_Click);
            // 
            // _limit
            // 
            this._limit.Location = new System.Drawing.Point(484, 12);
            this._limit.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this._limit.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this._limit.Name = "_limit";
            this._limit.Size = new System.Drawing.Size(72, 28);
            this._limit.TabIndex = 5;
            this._limit.Value = new decimal(new int[] {
            500,
            0,
            0,
            0});
            // 
            // _lblLimit
            // 
            this._lblLimit.AutoSize = true;
            this._lblLimit.Location = new System.Drawing.Point(444, 16);
            this._lblLimit.Name = "_lblLimit";
            this._lblLimit.Size = new System.Drawing.Size(44, 18);
            this._lblLimit.TabIndex = 4;
            this._lblLimit.Text = "条数";
            // 
            // _exchange
            // 
            this._exchange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._exchange.Items.AddRange(new object[] {
            "全部",
            "SSE",
            "SZSE",
            "BSE"});
            this._exchange.Location = new System.Drawing.Point(328, 12);
            this._exchange.Name = "_exchange";
            this._exchange.Size = new System.Drawing.Size(100, 26);
            this._exchange.TabIndex = 3;
            // 
            // _lblExchange
            // 
            this._lblExchange.AutoSize = true;
            this._lblExchange.Location = new System.Drawing.Point(276, 16);
            this._lblExchange.Name = "_lblExchange";
            this._lblExchange.Size = new System.Drawing.Size(62, 18);
            this._lblExchange.TabIndex = 2;
            this._lblExchange.Text = "交易所";
            // 
            // _keyword
            // 
            this._keyword.Location = new System.Drawing.Point(60, 12);
            this._keyword.Name = "_keyword";
            this._keyword.Size = new System.Drawing.Size(200, 28);
            this._keyword.TabIndex = 1;
            // 
            // _lblKeyword
            // 
            this._lblKeyword.AutoSize = true;
            this._lblKeyword.Location = new System.Drawing.Point(8, 16);
            this._lblKeyword.Name = "_lblKeyword";
            this._lblKeyword.Size = new System.Drawing.Size(62, 18);
            this._lblKeyword.TabIndex = 0;
            this._lblKeyword.Text = "关键字";
            // 
            // _treePanel
            // 
            this._treePanel.Controls.Add(this._tree);
            this._treePanel.Controls.Add(this._lblTreeTitle);
            this._treePanel.Dock = System.Windows.Forms.DockStyle.Left;
            this._treePanel.Location = new System.Drawing.Point(0, 52);
            this._treePanel.Name = "_treePanel";
            this._treePanel.Padding = new System.Windows.Forms.Padding(8, 8, 4, 8);
            this._treePanel.Size = new System.Drawing.Size(280, 557);
            this._treePanel.TabIndex = 1;
            // 
            // _tree
            // 
            this._tree.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tree.HideSelection = false;
            this._tree.Location = new System.Drawing.Point(8, 32);
            this._tree.Name = "_tree";
            this._tree.Size = new System.Drawing.Size(268, 517);
            this._tree.TabIndex = 1;
            this._tree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.Tree_AfterSelect);
            // 
            // _lblTreeTitle
            // 
            this._lblTreeTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this._lblTreeTitle.ForeColor = System.Drawing.Color.DimGray;
            this._lblTreeTitle.Location = new System.Drawing.Point(8, 8);
            this._lblTreeTitle.Name = "_lblTreeTitle";
            this._lblTreeTitle.Size = new System.Drawing.Size(268, 24);
            this._lblTreeTitle.TabIndex = 0;
            this._lblTreeTitle.Text = "行业分级（东财 EM 三级）";
            // 
            // _splitter
            // 
            this._splitter.Dock = System.Windows.Forms.DockStyle.Left;
            this._splitter.Location = new System.Drawing.Point(280, 52);
            this._splitter.MinExtra = 420;
            this._splitter.MinSize = 180;
            this._splitter.Name = "_splitter";
            this._splitter.Size = new System.Drawing.Size(4, 557);
            this._splitter.TabIndex = 2;
            // 
            // _body
            // 
            this._body.Controls.Add(this._grid);
            this._body.Controls.Add(this._hint);
            this._body.Dock = System.Windows.Forms.DockStyle.Fill;
            this._body.Location = new System.Drawing.Point(280, 52);
            this._body.Name = "_body";
            this._body.Size = new System.Drawing.Size(900, 557);
            this._body.TabIndex = 0;
            // 
            // _grid
            // 
            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToDeleteRows = false;
            this._grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.Location = new System.Drawing.Point(0, 0);
            this._grid.Name = "_grid";
            this._grid.ReadOnly = true;
            this._grid.RowHeadersWidth = 62;
            this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._grid.Size = new System.Drawing.Size(900, 557);
            this._grid.TabIndex = 1;
            this._grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Grid_CellDoubleClick);
            this._grid.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.Grid_CellMouseDown);
            this._grid.ContextMenuStrip = this._gridMenu;
            // 
            // _gridMenu
            // 
            this._gridMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._menuDetail});
            this._gridMenu.Name = "_gridMenu";
            this._gridMenu.Size = new System.Drawing.Size(124, 26);
            // 
            // _menuDetail
            // 
            this._menuDetail.Name = "_menuDetail";
            this._menuDetail.Size = new System.Drawing.Size(123, 22);
            this._menuDetail.Text = "详情";
            this._menuDetail.Click += new System.EventHandler(this.GridMenuDetail_Click);
            // 
            // _hint
            // 
            this._hint.Dock = System.Windows.Forms.DockStyle.Fill;
            this._hint.ForeColor = System.Drawing.Color.Gray;
            this._hint.Location = new System.Drawing.Point(0, 0);
            this._hint.Name = "_hint";
            this._hint.Size = new System.Drawing.Size(900, 557);
            this._hint.TabIndex = 0;
            this._hint.Text = "双击行查看完整详情";
            this._hint.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._hint.Visible = false;
            // 
            // _status
            // 
            this._status.ImageScalingSize = new System.Drawing.Size(24, 24);
            this._status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._statusText});
            this._status.Location = new System.Drawing.Point(0, 609);
            this._status.Name = "_status";
            this._status.Size = new System.Drawing.Size(1180, 31);
            this._status.TabIndex = 3;
            // 
            // _statusText
            // 
            this._statusText.Name = "_statusText";
            this._statusText.Size = new System.Drawing.Size(79, 24);
            this._statusText.Text = "准备中…";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1180, 640);
            this.Controls.Add(this._body);
            this.Controls.Add(this._splitter);
            this.Controls.Add(this._treePanel);
            this.Controls.Add(this._topPanel);
            this.Controls.Add(this._status);
            this.MinimumSize = new System.Drawing.Size(1100, 620);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "上市公司基础数据查询（Framework 4.6）";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this._topPanel.ResumeLayout(false);
            this._topPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._limit)).EndInit();
            this._treePanel.ResumeLayout(false);
            this._body.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this._status.ResumeLayout(false);
            this._status.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel _topPanel;
        private System.Windows.Forms.Label _lblKeyword;
        private System.Windows.Forms.TextBox _keyword;
        private System.Windows.Forms.Label _lblExchange;
        private System.Windows.Forms.ComboBox _exchange;
        private System.Windows.Forms.Label _lblLimit;
        private System.Windows.Forms.NumericUpDown _limit;
        private System.Windows.Forms.Button _btnSearch;
        private System.Windows.Forms.Button _btnReset;
        private System.Windows.Forms.Button _btnRefresh;
        private System.Windows.Forms.Button _btnInfoSources;
        private System.Windows.Forms.Button _btnVersions;
        private System.Windows.Forms.Panel _treePanel;
        private System.Windows.Forms.Label _lblTreeTitle;
        private System.Windows.Forms.TreeView _tree;
        private System.Windows.Forms.Splitter _splitter;
        private System.Windows.Forms.Panel _body;
        private System.Windows.Forms.Label _hint;
        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.ContextMenuStrip _gridMenu;
        private System.Windows.Forms.ToolStripMenuItem _menuDetail;
        private System.Windows.Forms.StatusStrip _status;
        private System.Windows.Forms.ToolStripStatusLabel _statusText;
    }
}
