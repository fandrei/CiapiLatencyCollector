using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatencyCollectorCore
{
	class MessageException : ApplicationException
	{
		public MessageException(string message)
			: base(message)
		{
		}

		public MessageException(string message, params object[] args)
			: base(string.Format(message, args))
		{
		}

		public MessageException(Exception exc, string message, params object[] args)
			: base(string.Format(message, args), exc)
		{
		}
	}
}
