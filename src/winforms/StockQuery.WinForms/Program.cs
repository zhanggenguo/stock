using System;
using System.Windows.Forms;

namespace StockQuery.WinForms
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var client = new ApiClient(ClientConfig.LoadApiBaseUrl());
                Application.Run(new MainForm(client));
            }
            catch (ApiException ex)
            {
                MessageBox.Show(ex.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
