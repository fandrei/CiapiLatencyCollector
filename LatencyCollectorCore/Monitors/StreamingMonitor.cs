using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore.Monitors
{
	class StreamingMonitor
	{
		static void EnsureStreamingStarted()
		{
			try
			{
				lock (StreamingSync)
				{
					if (_streamingData != null)
						return;

					var streamingData = new Data();
					streamingData.Login();
					streamingData.SubscribePrice(400481142); // GBP/USD

					_streamingData = streamingData;
				}
			}
			catch (Exception exc)
			{
				Program.Report(exc);
			}
		}

		static void StopStreaming()
		{
			try
			{
				lock (StreamingSync)
				{
					if (_streamingData == null)
						return;

					try
					{
						_streamingData.Logout();
					}
					catch (Exception exc)
					{
						Program.Report(exc);
					}

					_streamingData.Dispose();
					_streamingData = null;
				}
			}
			catch (Exception exc)
			{
				Program.Report(exc);
			}
		}

		private static Data _streamingData;
		private static readonly object StreamingSync = new object();
	}
}
