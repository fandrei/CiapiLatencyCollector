using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CiapiLatencyCollector;
using LatencyCollectorCore;

namespace LatencyCollectorConfig
{
	public partial class ConfigForm : Form
	{
		public ConfigForm()
		{
			InitializeComponent();
			Text = Const.AppName;
		}

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				base.OnLoad(e);

				MonitorsEdit.Text = AppSettings.Instance.GetMonitors();
			}
			catch (Exception exc)
			{
				MessageBox.Show(this, exc.ToString(), Const.AppName);
			}
		}

		private void OkButton_Click(object sender, EventArgs e)
		{
			try
			{
				AppSettings.Instance.SetMonitors(MonitorsEdit.Text);

				Close();
			}
			catch (Exception exc)
			{
				MessageBox.Show(this, exc.ToString(), Const.AppName);
			}
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
