﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using CiapiLatencyCollector;
using LatencyCollectorCore;
using LatencyCollectorCore.Monitors;

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

				ServerUrlEdit.Text = AppSettings.Instance.ServerUrl;
				StreamingUrlEdit.Text = AppSettings.Instance.StreamingServerUrl;

				UserNameEdit.Text = AppSettings.Instance.UserName;
				PasswordEdit.Text = AppSettings.Instance.Password;

				MonitorsEdit.Text = MonitorInfo.ToString(AppSettings.Instance.Monitors);
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
				AppSettings.Instance.ServerUrl = ServerUrlEdit.Text;
				AppSettings.Instance.StreamingServerUrl = StreamingUrlEdit.Text;

				AppSettings.Instance.UserName = UserNameEdit.Text;
				AppSettings.Instance.Password = PasswordEdit.Text;

				AppSettings.Instance.Monitors = MonitorInfo.Parse(MonitorsEdit.Text);

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
