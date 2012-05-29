using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public LatencyMonitor[] Monitors { get; set; }

		public string UserId { get; set; }

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
				Save();
			}

			if (Monitors == null)
			{
				if (!string.IsNullOrEmpty(UserName))
				{
					var monitor = new AllServiceMonitor
						{
							UserName = UserName,
							Password = Password,
							ServerUrl = ServerUrl,
							StreamingServerUrl = StreamingServerUrl,
							PeriodSeconds = (int)(60.0 / DataPollingRate),
						};
					Monitors = new LatencyMonitor[] { monitor };
					Save();
				}
				else
					Monitors = new LatencyMonitor[] { new DefaultPageMonitor(), new AllServiceMonitor() };
			}
		}

		private void SetDefaults()
		{
		}

		private static XmlSerializer CreateSerializer(XmlAttributeOverrides overrides = null)
		{
			if (overrides == null)
				overrides = new XmlAttributeOverrides();

			overrides.Add(typeof(AuthenticatedMonitor), "PasswordEncrypted", new XmlAttributes { XmlIgnore = true });
			overrides.Add(typeof(AuthenticatedMonitor), "Password", new XmlAttributes { XmlIgnore = false });

			var rootAttr = new XmlRootAttribute("Monitors");
			return new XmlSerializer(typeof(LatencyMonitor[]), overrides, new Type[0], rootAttr, "");
		}

		public void SetMonitors(string text)
		{
			var s = CreateSerializer();
			using (var rd = new StringReader(text))
			{
				var tmp = s.Deserialize(rd);
				Monitors = (LatencyMonitor[])tmp;
			}

			Save();
		}

		public string GetMonitors()
		{
			var s = CreateSerializer();
			using (var stringWriter = new StringWriter())
			{
				var writerSettings = new XmlWriterSettings
				{
					OmitXmlDeclaration = true,
					Indent = true,
				};
				using (var xmlWriter = XmlWriter.Create(stringWriter, writerSettings))
				{
					s.Serialize(xmlWriter, Monitors);
				}
				return stringWriter.ToString();
			}
		}

		#region Conversion from old versions

		public string ServerUrl { get; set; }
		public string StreamingServerUrl { get; set; }

		public int DataPollingRate { get; set; }

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

		#endregion
	}
}
