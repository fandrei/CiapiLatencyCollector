using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using CiapiLatencyCollector;
using LatencyCollectorCore.Monitors;

namespace LatencyCollectorCore
{
	public class AppSettings
	{
		public AppSettings()
		{
			SetDefaults();
		}

		#region Updating settings from the central repository

		public string ConfigBaseUrl { get; set; }

		public string UserName { get; set; }

		public string PasswordEncrypted { get; set; }
		static readonly byte[] AdditionalEntropy = { 0x43, 0x71, 0xDE, 0x5B, 0x44, 0x72, 0x45, 0xE3, 0xBE, 0x1E, 0x98, 0x2B, 0xAA };

		[XmlIgnore]
		public string Password
		{
			get
			{
				if (PasswordEncrypted.IsNullOrEmpty())
					return "";

				var encrypted = Convert.FromBase64String(PasswordEncrypted);
				var data = ProtectedData.Unprotect(encrypted, AdditionalEntropy, DataProtectionScope.LocalMachine);
				var res = Encoding.UTF8.GetString(data);
				return res;
			}
			set
			{
				var data = Encoding.UTF8.GetBytes(value);
				var encrypted = ProtectedData.Protect(data, AdditionalEntropy, DataProtectionScope.LocalMachine);
				PasswordEncrypted = Convert.ToBase64String(encrypted);
			}
		}

		private string _lastConfigText;

		public bool CheckRemoteSettings()
		{
			try
			{
				if (string.IsNullOrEmpty(UserName))
					return false;

				var text = DownloadConfigText();
				if (text == _lastConfigText)
					return false;

				if (_monitorSettings != null)
				{
					_monitorSettings.Dispose();
				}

				ApplyRemoteSettings(text);
				_lastConfigText = text;

				return true;
			}
			catch (Exception exc)
			{
				Program.Tracker.Log(exc);
			}
			return false;
		}

		private string DownloadConfigText()
		{
			var configAddress = string.Format(ConfigBaseUrl + "/CIAPILatencyCollectorConfig/{0}/AppSettings.xml", NodeName);
			var defaultConfigAddress = ConfigBaseUrl + "/CIAPILatencyCollectorConfig/AppSettings.xml";

			using (var client = new WebClient())
			{
				client.Credentials = new NetworkCredential(UserName, Password);

				try
				{
					return client.DownloadString(configAddress);
				}
				catch (WebException exc)
				{
					if (exc.Status == WebExceptionStatus.ProtocolError)
					{
						if (((HttpWebResponse)exc.Response).StatusCode == HttpStatusCode.NotFound)
						{
							return client.DownloadString(defaultConfigAddress);
						}
					}
					throw;
				}
			}
		}

		void ApplyRemoteSettings(string text)
		{
			var serializer = new XmlSerializer(typeof(MonitorSettings));

			using (var rd = new StringReader(text))
			{
				_monitorSettings = (MonitorSettings)serializer.Deserialize(rd);
			}
		}

		#endregion

		private MonitorSettings _monitorSettings = new MonitorSettings();

		public MonitorSettings MonitorSettings
		{
			get { return _monitorSettings; }
		}

		public string UserId { get; set; }

		public string NodeName { get; set; }

		private static AppSettings _instance;

		public static AppSettings Instance
		{
			get { return _instance ?? (_instance = Load()); }
		}

		public static string Version
		{
			get
			{
				var res = string.Format("v{0}", Assembly.GetCallingAssembly().GetName().Version);
				return res;
			}
		}

		private static readonly string FileName = Const.WorkingAreaPath + "AppSettings.xml";

		public static void Reload()
		{
			_instance = Load();
		}

		public static AppSettings Load()
		{
			AppSettings settings;

			if (File.Exists(FileName))
			{
				var s = new XmlSerializer(typeof(AppSettings));
				using (var rd = new StreamReader(FileName))
				{
					settings = (AppSettings)s.Deserialize(rd);
				}
			}
			else
				settings = new AppSettings();

			settings.SetDefaultsIfEmpty();
			settings.UpdateConfigVersion();

			return settings;
		}

		public void Save()
		{
			var directory = Path.GetDirectoryName(FileName);
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			var s = new XmlSerializer(typeof(AppSettings));
			using (var writer = new StreamWriter(FileName))
			{
				s.Serialize(writer, this);
			}
		}

		private void SetDefaultsIfEmpty()
		{
			if (UserId.IsNullOrEmpty())
			{
				UserId = Guid.NewGuid().ToString();
			}

			if (string.IsNullOrEmpty(ConfigBaseUrl))
				ConfigBaseUrl = "http://config.metrics.labs.cityindex.com";
		}

		private void SetDefaults()
		{
		}

		#region Conversion from old versions

		public int ConfigVersion { get; set; }

		private const int ActualConfigVersion = 1;

		void UpdateConfigVersion()
		{
			if (ConfigVersion == 0)
			{
				ConfigVersion = ActualConfigVersion;
				UserName = "";
				Password = "";
			}
		}

		#endregion
	}
}
