using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore.Tests
{
	static class TestSettings
	{
		public static string CiapiTestUserName
		{
			get
			{
				return Environment.GetEnvironmentVariable("CiapiTestUserName");
			}
		}

		public static string CiapiTestPassword
		{
			get
			{
				return Environment.GetEnvironmentVariable("CiapiTestPassword");
			}
		}

		public static string CiapiTestUrl
		{
			get
			{
				return Environment.GetEnvironmentVariable("CiapiTestUrl");
			}
		}

		public static string CiapiTestStreamingUrl
		{
			get
			{
				return Environment.GetEnvironmentVariable("CiapiTestStreamingUrl");
			}
		}
	}
}
