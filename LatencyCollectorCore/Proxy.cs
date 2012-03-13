using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LatencyCollectorCore
{
	public class Proxy : MarshalByRefObject
	{
		public Proxy(string typeName, string funcName, object[] args)
		{
			var assembly = Assembly.GetExecutingAssembly();
			var type = assembly.GetType(typeName);
			if (type == null)
				throw new ApplicationException(string.Format("Type {0} not found", typeName));

			type.InvokeMember(funcName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, args);
		}
	}
}
