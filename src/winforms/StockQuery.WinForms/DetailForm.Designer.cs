namespace StockQuery.WinForms
{
    partial class DetailForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>设计器支持所需的方法，可用 VS 设计器直接调整 Tab、表格与图表。</summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewTextBoxColumn colTradeDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colClose = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colPctChange = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colAmplitude = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colVolume = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colTurnover = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colTotalCapYi = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colFloatCapYi = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colPeTtm = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colPb = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn colQuality = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartAreaPrice = new System.Windows.Forms.DataVisualization.Charting.ChartArea("PriceArea");
            System.Windows.Forms.DataVisualization.Charting.Series seriesClose = new System.Windows.Forms.DataVisualization.Charting.Series("收盘价");
            System.Windows.Forms.DataVisualization.Charting.Title chartTitle = new System.Windows.Forms.DataVisualization.Charting.Title("历史收盘价走势");
            this._tabs = new System.Windows.Forms.TabControl();
            this._tabPrice = new System.Windows.Forms.TabPage();
            this._gridMarket = new System.Windows.Forms.DataGridView();
            this._chart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this._lblPriceHint = new System.Windows.Forms.Label();
            this._tabCompany = new System.Windows.Forms.TabPage();
            this._panel = new System.Windows.Forms.Panel();
            this._btnClose = new System.Windows.Forms.Button();
            this._tabs.SuspendLayout();
            this._tabPrice.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._gridMarket)).BeginInit();
            this._tabCompany.SuspendLayout();
            this.SuspendLayout();
            //
            // _tabs
            //
            this._tabs.Controls.Add(this._tabPrice);
            this._tabs.Controls.Add(this._tabCompany);
            this._tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tabs.Location = new System.Drawing.Point(0, 0);
            this._tabs.Name = "_tabs";
            this._tabs.SelectedIndex = 0;
            this._tabs.Size = new System.Drawing.Size(1000, 684);
            this._tabs.TabIndex = 0;
            //
            // _tabPrice
            //
            this._tabPrice.Controls.Add(this._gridMarket);
            this._tabPrice.Controls.Add(this._chart);
            this._tabPrice.Controls.Add(this._lblPriceHint);
            this._tabPrice.Location = new System.Drawing.Point(4, 24);
            this._tabPrice.Name = "_tabPrice";
            this._tabPrice.Padding = new System.Windows.Forms.Padding(3);
            this._tabPrice.Size = new System.Drawing.Size(992, 656);
            this._tabPrice.TabIndex = 0;
            this._tabPrice.Text = "历史股价";
            this._tabPrice.UseVisualStyleBackColor = true;
            //
            // colTradeDate
            //
            colTradeDate.DataPropertyName = "TradeDate";
            colTradeDate.DefaultCellStyle.Format = "yyyy-MM-dd";
            colTradeDate.HeaderText = "交易日";
            colTradeDate.Name = "colTradeDate";
            colTradeDate.Width = 100;
            //
            // colClose
            //
            colClose.DataPropertyName = "Close";
            colClose.DefaultCellStyle.Format = "#,##0.00";
            colClose.HeaderText = "收盘价";
            colClose.Name = "colClose";
            colClose.Width = 84;
            //
            // colPctChange
            //
            colPctChange.DataPropertyName = "PctChange";
            colPctChange.DefaultCellStyle.Format = "0.00";
            colPctChange.HeaderText = "涨跌幅(%)";
            colPctChange.Name = "colPctChange";
            colPctChange.Width = 92;
            //
            // colAmplitude
            //
            colAmplitude.DataPropertyName = "Amplitude";
            colAmplitude.DefaultCellStyle.Format = "0.00";
            colAmplitude.HeaderText = "振幅(%)";
            colAmplitude.Name = "colAmplitude";
            colAmplitude.Width = 84;
            //
            // colVolume
            //
            colVolume.DataPropertyName = "Volume";
            colVolume.DefaultCellStyle.Format = "#,##0";
            colVolume.HeaderText = "成交量";
            colVolume.Name = "colVolume";
            colVolume.Width = 108;
            //
            // colAmount
            //
            colAmount.DataPropertyName = "Amount";
            colAmount.DefaultCellStyle.Format = "#,##0";
            colAmount.HeaderText = "成交额(元)";
            colAmount.Name = "colAmount";
            colAmount.Width = 130;
            //
            // colTurnover
            //
            colTurnover.DataPropertyName = "TurnoverRate";
            colTurnover.DefaultCellStyle.Format = "0.00";
            colTurnover.HeaderText = "换手率(%)";
            colTurnover.Name = "colTurnover";
            colTurnover.Width = 92;
            //
            // colTotalCapYi
            //
            colTotalCapYi.DataPropertyName = "TotalMarketCapYi";
            colTotalCapYi.DefaultCellStyle.Format = "#,##0.00";
            colTotalCapYi.HeaderText = "总市值(亿)";
            colTotalCapYi.Name = "colTotalCapYi";
            colTotalCapYi.Width = 106;
            //
            // colFloatCapYi
            //
            colFloatCapYi.DataPropertyName = "FloatMarketCapYi";
            colFloatCapYi.DefaultCellStyle.Format = "#,##0.00";
            colFloatCapYi.HeaderText = "流通市值(亿)";
            colFloatCapYi.Name = "colFloatCapYi";
            colFloatCapYi.Width = 112;
            //
            // colPeTtm
            //
            colPeTtm.DataPropertyName = "PeTtm";
            colPeTtm.DefaultCellStyle.Format = "0.00";
            colPeTtm.HeaderText = "PE(TTM)";
            colPeTtm.Name = "colPeTtm";
            colPeTtm.Width = 78;
            //
            // colPb
            //
            colPb.DataPropertyName = "Pb";
            colPb.DefaultCellStyle.Format = "0.00";
            colPb.HeaderText = "PB";
            colPb.Name = "colPb";
            colPb.Width = 64;
            //
            // colQuality
            //
            colQuality.DataPropertyName = "QualityFlag";
            colQuality.HeaderText = "数据标记";
            colQuality.Name = "colQuality";
            colQuality.Width = 130;
            //
            // _gridMarket
            //
            this._gridMarket.AllowUserToAddRows = false;
            this._gridMarket.AllowUserToDeleteRows = false;
            this._gridMarket.AutoGenerateColumns = false;
            this._gridMarket.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            colTradeDate,
            colClose,
            colPctChange,
            colAmplitude,
            colVolume,
            colAmount,
            colTurnover,
            colTotalCapYi,
            colFloatCapYi,
            colPeTtm,
            colPb,
            colQuality});
            this._gridMarket.Dock = System.Windows.Forms.DockStyle.Fill;
            this._gridMarket.Location = new System.Drawing.Point(3, 3);
            this._gridMarket.Name = "_gridMarket";
            this._gridMarket.ReadOnly = true;
            this._gridMarket.RowHeadersWidth = 60;
            this._gridMarket.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._gridMarket.Size = new System.Drawing.Size(986, 406);
            this._gridMarket.TabIndex = 0;
            //
            // chartAreaPrice
            //
            chartAreaPrice.AxisX.Interval = 1;
            chartAreaPrice.AxisX.LabelStyle.Format = "MM-dd";
            chartAreaPrice.AxisX.MajorGrid.Enabled = false;
            chartAreaPrice.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            chartAreaPrice.AxisY.Title = "收盘价(元)";
            chartAreaPrice.Name = "PriceArea";
            //
            // seriesClose
            //
            seriesClose.BorderWidth = 2;
            seriesClose.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            seriesClose.MarkerSize = 5;
            seriesClose.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Circle;
            seriesClose.Name = "收盘价";
            seriesClose.XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;
            seriesClose.YValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Double;
            //
            // chartTitle
            //
            chartTitle.Name = "TitlePrice";
            chartTitle.Text = "历史收盘价走势";
            //
            // _chart
            //
            this._chart.ChartAreas.Add(chartAreaPrice);
            this._chart.Series.Add(seriesClose);
            this._chart.Titles.Add(chartTitle);
            this._chart.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._chart.Location = new System.Drawing.Point(3, 409);
            this._chart.Name = "_chart";
            this._chart.Size = new System.Drawing.Size(986, 244);
            this._chart.TabIndex = 1;
            //
            // _lblPriceHint
            //
            this._lblPriceHint.AutoSize = false;
            this._lblPriceHint.BackColor = System.Drawing.Color.White;
            this._lblPriceHint.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lblPriceHint.ForeColor = System.Drawing.Color.Gray;
            this._lblPriceHint.Location = new System.Drawing.Point(3, 3);
            this._lblPriceHint.Name = "_lblPriceHint";
            this._lblPriceHint.Size = new System.Drawing.Size(986, 650);
            this._lblPriceHint.TabIndex = 2;
            this._lblPriceHint.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._lblPriceHint.Visible = false;
            //
            // _tabCompany
            //
            this._tabCompany.Controls.Add(this._panel);
            this._tabCompany.Location = new System.Drawing.Point(4, 24);
            this._tabCompany.Name = "_tabCompany";
            this._tabCompany.Padding = new System.Windows.Forms.Padding(3);
            this._tabCompany.Size = new System.Drawing.Size(992, 656);
            this._tabCompany.TabIndex = 1;
            this._tabCompany.Text = "公司资料";
            this._tabCompany.UseVisualStyleBackColor = true;
            //
            // _panel
            //
            this._panel.AutoScroll = true;
            this._panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._panel.Location = new System.Drawing.Point(3, 3);
            this._panel.Name = "_panel";
            this._panel.Padding = new System.Windows.Forms.Padding(12);
            this._panel.Size = new System.Drawing.Size(986, 650);
            this._panel.TabIndex = 0;
            //
            // _btnClose
            //
            this._btnClose.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._btnClose.Location = new System.Drawing.Point(0, 684);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(1000, 36);
            this._btnClose.TabIndex = 1;
            this._btnClose.Text = "关闭";
            this._btnClose.UseVisualStyleBackColor = true;
            this._btnClose.Click += new System.EventHandler(this.Close_Click);
            //
            // DetailForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 720);
            this.Controls.Add(this._tabs);
            this.Controls.Add(this._btnClose);
            this.MinimumSize = new System.Drawing.Size(760, 560);
            this.Name = "DetailForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "公司详情";
            this._tabs.ResumeLayout(false);
            this._tabPrice.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._gridMarket)).EndInit();
            this._tabCompany.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl _tabs;
        private System.Windows.Forms.TabPage _tabPrice;
        private System.Windows.Forms.DataGridView _gridMarket;
        private System.Windows.Forms.DataVisualization.Charting.Chart _chart;
        private System.Windows.Forms.Label _lblPriceHint;
        private System.Windows.Forms.TabPage _tabCompany;
        private System.Windows.Forms.Panel _panel;
        private System.Windows.Forms.Button _btnClose;
    }
}
