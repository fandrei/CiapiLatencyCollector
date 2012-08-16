using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace LatencyCollectorConfigSite
{
	/// <summary>
	/// Summary description for GetConfig
	/// </summary>
	public class GetConfig : IHttpHandler
	{
		public void ProcessRequest(HttpContext context)
		{
			context.Response.ContentType = "text/plain";

			var configText = "";

			if (File.Exists(Const.StopFileName))
			{
				configText = "disabled";
			}
			else
			{
				var nodeName = context.Request.Params.Get("NodeName");
				var configPath = Const.ConfigBasePath + nodeName + "/" + Const.NodeSettingsFileName;
				if (File.Exists(configPath))
				{
					configText = File.ReadAllText(configPath);
				}
				else
				{
					var defaultConfigPath = Const.ConfigBasePath + Const.NodeSettingsFileName;
					if (File.Exists(defaultConfigPath))
						configText = File.ReadAllText(defaultConfigPath);
				}
			}

			context.Response.Write(configText);
		}

		public bool IsReusable
		{
			get
			{
				return false;
			}
		}
	}
}