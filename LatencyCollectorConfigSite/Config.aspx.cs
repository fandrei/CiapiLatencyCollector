using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LatencyCollectorConfigSite
{
	public partial class Config : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			RefreshButtonsState();
		}

		private void RefreshButtonsState()
		{
			var fileExists = File.Exists(Const.StopFileName);
			EnableButton.Enabled = fileExists;
			DisableButton.Enabled = !fileExists;
		}

		protected void EnableButton_Click(object sender, EventArgs e)
		{
			File.Delete(Const.StopFileName);
			RefreshButtonsState();
		}

		protected void DisableButton_Click(object sender, EventArgs e)
		{
			File.WriteAllText(Const.StopFileName, "");

			RefreshButtonsState();
		}
	}
}