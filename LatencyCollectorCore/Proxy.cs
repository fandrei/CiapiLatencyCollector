using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore
{
	public class Proxy : MarshalByRefObject
	{
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
