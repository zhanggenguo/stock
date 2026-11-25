namespace StockQuery.WinForms
{
    partial class InfoSourceForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>设计器支持所需的方法，可用 VS 设计器直接调整表格与编辑区。</summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewTextBoxColumn colId = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colCategory = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colEntryType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colSiteName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colKeyContent = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colUrl = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colUpdatedAt = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._filterPanel = new System.Windows.Forms.Panel();
            this._lblFilter = new System.Windows.Forms.Label();
            this._cboFilterCategory = new System.Windows.Forms.ComboBox();
            this._grid = new System.Windows.Forms.DataGridView();
            this._editPanel = new System.Windows.Forms.Panel();
            this._lblCategory = new System.Windows.Forms.Label();
            this._txtCategory = new System.Windows.Forms.TextBox();
            this._lblEntryType = new System.Windows.Forms.Label();
            this._cboEntryType = new System.Windows.Forms.ComboBox();
            this._lblName = new System.Windows.Forms.Label();
            this._txtName = new System.Windows.Forms.TextBox();
            this._lblUrl = new System.Windows.Forms.Label();
            this._txtUrl = new System.Windows.Forms.TextBox();
            this._lblDesc = new System.Windows.Forms.Label();
            this._txtDesc = new System.Windows.Forms.TextBox();
            this._lblKey = new System.Windows.Forms.Label();
            this._txtKey = new System.Windows.Forms.TextBox();
            this._btnAdd = new System.Windows.Forms.Button();
            this._btnSave = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._btnDelete = new System.Windows.Forms.Button();
            this._btnClose = new System.Windows.Forms.Button();
            this._hint = new System.Windows.Forms.Label();
            this._filterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).BeginInit();
            this._editPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // colId
            //
            colId.DataPropertyName = "Id";
            colId.HeaderText = "ID";
            colId.Name = "colId";
            colId.Width = 55;
            //
            // colCategory
            //
            colCategory.DataPropertyName = "Category";
            colCategory.HeaderText = "分类";
            colCategory.Name = "colCategory";
            colCategory.Width = 170;
            //
            // colEntryType
            //
            colEntryType.DataPropertyName = "EntryType";
            colEntryType.HeaderText = "类型";
            colEntryType.Name = "colEntryType";
            colEntryType.Width = 62;
            //
            // colSiteName
            //
            colSiteName.DataPropertyName = "SiteName";
            colSiteName.HeaderText = "名称";
            colSiteName.Name = "colSiteName";
            colSiteName.Width = 150;
            //
            // colDescription
            //
            colDescription.DataPropertyName = "Description";
            colDescription.HeaderText = "定位描述 / 说明";
            colDescription.Name = "colDescription";
            colDescription.Width = 250;
            //
            // colKeyContent
            //
            colKeyContent.DataPropertyName = "KeyContent";
            colKeyContent.HeaderText = "重点查询内容 / 组合 / 目的";
            colKeyContent.Name = "colKeyContent";
            colKeyContent.Width = 200;
            //
            // colUrl
            //
            colUrl.DataPropertyName = "Url";
            colUrl.HeaderText = "网页地址";
            colUrl.Name = "colUrl";
            colUrl.Width = 210;
            //
            // colUpdatedAt
            //
            colUpdatedAt.DataPropertyName = "UpdatedAt";
            colUpdatedAt.HeaderText = "更新时间";
            colUpdatedAt.Name = "colUpdatedAt";
            colUpdatedAt.Width = 130;
            //
            // _filterPanel
            //
            this._filterPanel.Controls.Add(this._lblFilter);
            this._filterPanel.Controls.Add(this._cboFilterCategory);
            this._filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._filterPanel.Location = new System.Drawing.Point(0, 0);
            this._filterPanel.Name = "_filterPanel";
            this._filterPanel.Size = new System.Drawing.Size(1160, 40);
            this._filterPanel.TabIndex = 2;
            //
            // _lblFilter
            //
            this._lblFilter.AutoSize = true;
            this._lblFilter.Location = new System.Drawing.Point(16, 11);
            this._lblFilter.Name = "_lblFilter";
            this._lblFilter.Size = new System.Drawing.Size(76, 18);
            this._lblFilter.TabIndex = 0;
            this._lblFilter.Text = "分类筛选";
            //
            // _cboFilterCategory
            //
            this._cboFilterCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboFilterCategory.DropDownWidth = 420;
            this._cboFilterCategory.Location = new System.Drawing.Point(100, 7);
            this._cboFilterCategory.Name = "_cboFilterCategory";
            this._cboFilterCategory.Size = new System.Drawing.Size(420, 26);
            this._cboFilterCategory.TabIndex = 1;
            this._cboFilterCategory.SelectedIndexChanged += new System.EventHandler(this.FilterCategory_SelectedIndexChanged);
            //
            // _grid
            //
            this._grid.AllowUserToAddRows = false;
            this._grid.AllowUserToDeleteRows = false;
            this._grid.AutoGenerateColumns = false;
            this._grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            colId,
            colCategory,
            colEntryType,
            colSiteName,
            colDescription,
            colKeyContent,
            colUrl,
            colUpdatedAt});
            this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this._grid.Location = new System.Drawing.Point(0, 40);
            this._grid.Name = "_grid";
            this._grid.ReadOnly = true;
            this._grid.RowHeadersWidth = 56;
            this._grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._grid.Size = new System.Drawing.Size(1160, 430);
            this._grid.TabIndex = 0;
            this._grid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Grid_CellDoubleClick);
            this._grid.SelectionChanged += new System.EventHandler(this.Grid_SelectionChanged);
            //
            // _editPanel
            //
            this._editPanel.Controls.Add(this._lblCategory);
            this._editPanel.Controls.Add(this._txtCategory);
            this._editPanel.Controls.Add(this._lblEntryType);
            this._editPanel.Controls.Add(this._cboEntryType);
            this._editPanel.Controls.Add(this._lblName);
            this._editPanel.Controls.Add(this._txtName);
            this._editPanel.Controls.Add(this._lblUrl);
            this._editPanel.Controls.Add(this._txtUrl);
            this._editPanel.Controls.Add(this._lblDesc);
            this._editPanel.Controls.Add(this._txtDesc);
            this._editPanel.Controls.Add(this._lblKey);
            this._editPanel.Controls.Add(this._txtKey);
            this._editPanel.Controls.Add(this._btnAdd);
            this._editPanel.Controls.Add(this._btnSave);
            this._editPanel.Controls.Add(this._btnCancel);
            this._editPanel.Controls.Add(this._btnDelete);
            this._editPanel.Controls.Add(this._btnClose);
            this._editPanel.Controls.Add(this._hint);
            this._editPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._editPanel.Location = new System.Drawing.Point(0, 470);
            this._editPanel.Name = "_editPanel";
            this._editPanel.Size = new System.Drawing.Size(1160, 230);
            this._editPanel.TabIndex = 1;
            //
            // _lblCategory
            //
            this._lblCategory.AutoSize = true;
            this._lblCategory.Location = new System.Drawing.Point(16, 18);
            this._lblCategory.Name = "_lblCategory";
            this._lblCategory.Size = new System.Drawing.Size(58, 18);
            this._lblCategory.TabIndex = 0;
            this._lblCategory.Text = "分类";
            //
            // _txtCategory
            //
            this._txtCategory.Location = new System.Drawing.Point(100, 14);
            this._txtCategory.Name = "_txtCategory";
            this._txtCategory.Size = new System.Drawing.Size(340, 26);
            this._txtCategory.TabIndex = 1;
            //
            // _lblEntryType
            //
            this._lblEntryType.AutoSize = true;
            this._lblEntryType.Location = new System.Drawing.Point(460, 18);
            this._lblEntryType.Name = "_lblEntryType";
            this._lblEntryType.Size = new System.Drawing.Size(58, 18);
            this._lblEntryType.TabIndex = 2;
            this._lblEntryType.Text = "类型";
            //
            // _cboEntryType
            //
            this._cboEntryType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboEntryType.Items.AddRange(new object[] {
            "网站",
            "组合",
            "模板",
            "笔记"});
            this._cboEntryType.Location = new System.Drawing.Point(530, 14);
            this._cboEntryType.Name = "_cboEntryType";
            this._cboEntryType.Size = new System.Drawing.Size(110, 26);
            this._cboEntryType.TabIndex = 3;
            //
            // _lblName
            //
            this._lblName.AutoSize = true;
            this._lblName.Location = new System.Drawing.Point(16, 56);
            this._lblName.Name = "_lblName";
            this._lblName.Size = new System.Drawing.Size(58, 18);
            this._lblName.TabIndex = 4;
            this._lblName.Text = "名称";
            //
            // _txtName
            //
            this._txtName.Location = new System.Drawing.Point(100, 52);
            this._txtName.Name = "_txtName";
            this._txtName.Size = new System.Drawing.Size(340, 26);
            this._txtName.TabIndex = 5;
            //
            // _lblUrl
            //
            this._lblUrl.AutoSize = true;
            this._lblUrl.Location = new System.Drawing.Point(460, 56);
            this._lblUrl.Name = "_lblUrl";
            this._lblUrl.Size = new System.Drawing.Size(76, 18);
            this._lblUrl.TabIndex = 6;
            this._lblUrl.Text = "网页地址";
            //
            // _txtUrl
            //
            this._txtUrl.Location = new System.Drawing.Point(560, 52);
            this._txtUrl.Name = "_txtUrl";
            this._txtUrl.Size = new System.Drawing.Size(580, 26);
            this._txtUrl.TabIndex = 7;
            //
            // _lblDesc
            //
            this._lblDesc.AutoSize = true;
            this._lblDesc.Location = new System.Drawing.Point(16, 94);
            this._lblDesc.Name = "_lblDesc";
            this._lblDesc.Size = new System.Drawing.Size(76, 18);
            this._lblDesc.TabIndex = 8;
            this._lblDesc.Text = "定位描述";
            //
            // _txtDesc
            //
            this._txtDesc.Location = new System.Drawing.Point(100, 90);
            this._txtDesc.Name = "_txtDesc";
            this._txtDesc.Size = new System.Drawing.Size(420, 26);
            this._txtDesc.TabIndex = 9;
            //
            // _lblKey
            //
            this._lblKey.AutoSize = true;
            this._lblKey.Location = new System.Drawing.Point(540, 94);
            this._lblKey.Name = "_lblKey";
            this._lblKey.Size = new System.Drawing.Size(114, 18);
            this._lblKey.TabIndex = 10;
            this._lblKey.Text = "重点查询内容";
            //
            // _txtKey
            //
            this._txtKey.Location = new System.Drawing.Point(660, 90);
            this._txtKey.Name = "_txtKey";
            this._txtKey.Size = new System.Drawing.Size(480, 26);
            this._txtKey.TabIndex = 11;
            //
            // _btnAdd
            //
            this._btnAdd.Location = new System.Drawing.Point(16, 130);
            this._btnAdd.Name = "_btnAdd";
            this._btnAdd.Size = new System.Drawing.Size(84, 32);
            this._btnAdd.TabIndex = 12;
            this._btnAdd.Text = "新增";
            this._btnAdd.UseVisualStyleBackColor = true;
            this._btnAdd.Click += new System.EventHandler(this.Add_Click);
            //
            // _btnSave
            //
            this._btnSave.Location = new System.Drawing.Point(110, 130);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Size = new System.Drawing.Size(84, 32);
            this._btnSave.TabIndex = 13;
            this._btnSave.Text = "保存";
            this._btnSave.UseVisualStyleBackColor = true;
            this._btnSave.Click += new System.EventHandler(this.Save_Click);
            //
            // _btnCancel
            //
            this._btnCancel.Location = new System.Drawing.Point(204, 130);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(84, 32);
            this._btnCancel.TabIndex = 14;
            this._btnCancel.Text = "取消编辑";
            this._btnCancel.UseVisualStyleBackColor = true;
            this._btnCancel.Click += new System.EventHandler(this.Cancel_Click);
            //
            // _btnDelete
            //
            this._btnDelete.Location = new System.Drawing.Point(298, 130);
            this._btnDelete.Name = "_btnDelete";
            this._btnDelete.Size = new System.Drawing.Size(84, 32);
            this._btnDelete.TabIndex = 15;
            this._btnDelete.Text = "删除";
            this._btnDelete.UseVisualStyleBackColor = true;
            this._btnDelete.Click += new System.EventHandler(this.Delete_Click);
            //
            // _btnClose
            //
            this._btnClose.Location = new System.Drawing.Point(1056, 130);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(84, 32);
            this._btnClose.TabIndex = 16;
            this._btnClose.Text = "关闭";
            this._btnClose.UseVisualStyleBackColor = true;
            this._btnClose.Click += new System.EventHandler(this.Close_Click);
            //
            // _hint
            //
            this._hint.AutoSize = false;
            this._hint.ForeColor = System.Drawing.Color.DimGray;
            this._hint.Location = new System.Drawing.Point(16, 176);
            this._hint.Name = "_hint";
            this._hint.Size = new System.Drawing.Size(1124, 22);
            this._hint.TabIndex = 17;
            this._hint.Text = "选择一行可修改或删除；点“新增”后填写信息并保存。";
            this._hint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // InfoSourceForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1160, 700);
            this.Controls.Add(this._grid);
            this.Controls.Add(this._editPanel);
            this.Controls.Add(this._filterPanel);
            this.MinimumSize = new System.Drawing.Size(900, 560);
            this.Name = "InfoSourceForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "信息来源目录";
            this.Load += new System.EventHandler(this.InfoSourceForm_Load);
            this._filterPanel.ResumeLayout(false);
            this._filterPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._grid)).EndInit();
            this._editPanel.ResumeLayout(false);
            this._editPanel.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel _filterPanel;
        private System.Windows.Forms.Label _lblFilter;
        private System.Windows.Forms.ComboBox _cboFilterCategory;
        private System.Windows.Forms.DataGridView _grid;
        private System.Windows.Forms.Panel _editPanel;
        private System.Windows.Forms.Label _lblCategory;
        private System.Windows.Forms.TextBox _txtCategory;
        private System.Windows.Forms.Label _lblEntryType;
        private System.Windows.Forms.ComboBox _cboEntryType;
        private System.Windows.Forms.Label _lblName;
        private System.Windows.Forms.TextBox _txtName;
        private System.Windows.Forms.Label _lblUrl;
        private System.Windows.Forms.TextBox _txtUrl;
        private System.Windows.Forms.Label _lblDesc;
        private System.Windows.Forms.TextBox _txtDesc;
        private System.Windows.Forms.Label _lblKey;
        private System.Windows.Forms.TextBox _txtKey;
        private System.Windows.Forms.Button _btnAdd;
        private System.Windows.Forms.Button _btnSave;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Button _btnDelete;
        private System.Windows.Forms.Button _btnClose;
        private System.Windows.Forms.Label _hint;
    }
}
