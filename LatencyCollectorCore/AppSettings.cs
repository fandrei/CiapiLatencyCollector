using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

using CiapiLatencyCollector;
using LatencyCollectorCore.Monitors;

namespace LatencyCollectorCore
{
	public class AppSettings : AppSettingsBase
	{
		public AppSettings()
		{
			SetDefaults();
		}

		#region Updating settings from the central repository

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

				if (text == null)
				{
					_monitorSettings.PollingDisabled = true;
				}
				else
				{
					_monitorSettings.Dispose();
					ApplyRemoteSettings(text);
				}

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
			var configAddress = ConfigBaseUrl + string.Format("/GetConfig.ashx?NodeName={0}", NodeName);

			using (var client = new WebClient())
			{
				client.Credentials = new NetworkCredential(UserName, Password);

				var res = client.DownloadString(configAddress);
				if (res == "disabled")
					return null;
				return res;
			}
		}

		private static XmlSerializer _serializer;

		void ApplyRemoteSettings(string text)
		{
			if (_serializer == null)
			{
				_serializer = new XmlSerializer(typeof(MonitorSettings));
			}

			using (var rd = new StringReader(text))
			{
				_monitorSettings = (MonitorSettings)_serializer.Deserialize(rd);
			}

			_monitorSettings.PollingDisabled = false;
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
			get { return _instance ?? (_instance = Load<AppSettings>()); }
		}

		public static string Version
		{
			get
			{
				var res = string.Format("v{0}", Assembly.GetCallingAssembly().GetName().Version);
				return res;
			}
		}

		protected override void OnAfterLoad()
		{
			if (UserId.IsNullOrEmpty())
			{
				UserId = Guid.NewGuid().ToString();
			}

			UpdateConfigVersion();
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
