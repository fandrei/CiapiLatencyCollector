using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace CiapiLatencyCollector
{
	public class AppSettingsBase
	{
		public AppSettingsBase()
		{
			ConfigBaseUrl = "http://config.metrics.labs.cityindex.com";
		}

		protected static readonly string FileName = Const.WorkingAreaPath + "AppSettings.xml";
		private static XmlSerializer _serializer;

		public static T Load<T>()
			where T : AppSettingsBase, new()
		{
			T settings;

			if (File.Exists(FileName))
			{
				var rootAttr = new XmlRootAttribute("AppSettings");
				if (_serializer == null)
				{
					_serializer = new XmlSerializer(typeof(T), null, null, rootAttr, "");
				}
				using (var rd = new StreamReader(FileName))
				{
					settings = (T)_serializer.Deserialize(rd);
				}
			}
			else
				settings = new T();

			settings.OnAfterLoad();

			return settings;
		}

		protected virtual void OnAfterLoad()
		{
		}

		public string ConfigBaseUrl { get; set; }

		public string AutoUpdateUrl
		{
			get { return ConfigBaseUrl + "/CIAPILatencyCollector/updates/"; }
		}
	}
}
