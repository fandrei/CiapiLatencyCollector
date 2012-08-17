using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using System.Web.UI;
using System.Web.UI.WebControls;

namespace LatencyCollectorConfigSite
{
	public partial class Config : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!Page.IsPostBack)
			{
				RefreshButtonsState();

				foreach (var cur in GetConfig.NodeNames)
				{
					var row = new TableRow();
					row.Cells.Add(new TableCell { Text = cur.Key });
					row.Cells.Add(new TableCell { Text = cur.Value });
					NodesList.Rows.Add(row);
				}
			}
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
			Page.Response.Redirect(Request.RawUrl);
		}

		protected void DisableButton_Click(object sender, EventArgs e)
		{
			File.WriteAllText(Const.StopFileName, "");
			RefreshButtonsState();
			Page.Response.Redirect(Request.RawUrl);
		}
	}
}