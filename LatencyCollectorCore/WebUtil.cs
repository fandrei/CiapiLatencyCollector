﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace LatencyCollectorCore
{
	class WebUtil
	{
		public static bool IsConnectionFailure(WebException exc)
			return false;
		public static bool IsNotFound(WebException exc)
			return false;
}