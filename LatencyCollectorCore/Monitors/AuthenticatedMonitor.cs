using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace LatencyCollectorCore.Monitors
{
	public abstract class AuthenticatedMonitor : LatencyMonitor
	{
		protected AuthenticatedMonitor()
		{
			UserName = "";
		}

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
	}
}
