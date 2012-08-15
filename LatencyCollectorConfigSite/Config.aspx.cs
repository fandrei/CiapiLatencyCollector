using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using AppMetrics.WebUtils;

namespace LatencyCollectorConfigSite
{
	public partial class Config : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			try
			{
				RefreshButtonsState();
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		private void RefreshButtonsState()
		{
			var fileExists = File.Exists(StopFileName);
			EnableButton.Enabled = fileExists;
			DisableButton.Enabled = !fileExists;
		}

		protected void EnableButton_Click(object sender, EventArgs e)
		{
			try
			{
				File.Delete(StopFileName);
				RefreshButtonsState();
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		protected void DisableButton_Click(object sender, EventArgs e)
		{
			try
			{
				File.WriteAllText(StopFileName, "");
				RefreshButtonsState();
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		private string StopFileName
		{
			get
			{
				const string tmp = "~/CIAPILatencyCollectorConfig/stop.txt";
				return HostingEnvironment.MapPath(tmp);
			}
		}
	}
}