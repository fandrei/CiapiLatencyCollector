using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore
{
	public class Proxy : MarshalByRefObject
	{
		// remote references to this object never expire
		public override object InitializeLifetimeService()
		{
			return null;
		}

		public void Start()
		{
			Program.Start();
		}

		public void Stop()
		{
			Program.Stop();
		}
	}
}
