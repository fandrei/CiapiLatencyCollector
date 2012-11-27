﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace LatencyCollectorCore
{
	class WebUtil
	{
		public static bool IsConnectionFailure(WebException exc)		{			if (exc.Status == WebExceptionStatus.NameResolutionFailure ||				exc.Status == WebExceptionStatus.Timeout ||				exc.Status == WebExceptionStatus.ConnectFailure ||				exc.Status == WebExceptionStatus.ConnectionClosed)			{				return true;			}
			return false;		}
		public static bool IsNotFound(WebException exc)		{			if (exc.Status == WebExceptionStatus.ProtocolError)			{				if (((HttpWebResponse)exc.Response).StatusCode == HttpStatusCode.NotFound)				{					return true;				}			}
			return false;		}	}
}
