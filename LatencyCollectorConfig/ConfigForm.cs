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

				UserNameEdit.Text = AppSettings.Instance.UserName;
				PasswordEdit.Text = AppSettings.Instance.Password;
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
				AppSettings.Instance.UserName = UserNameEdit.Text;
				AppSettings.Instance.Password = PasswordEdit.Text;

				AppSettings.Instance.Save();

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
