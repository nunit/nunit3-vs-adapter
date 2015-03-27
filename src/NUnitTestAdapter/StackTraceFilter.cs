// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

namespace NUnit.VisualStudio.TestAdapter
{
	using System;
	using System.IO;
	using System.Linq;

    /// <summary>
	/// Summary description for StackTraceFilter.
	/// </summary>
	public static class StackTraceFilter
	{
		public static string Filter(string stack) 
		{
			if(stack == null) return null;
			var sw = new StringWriter();
			var sr = new StringReader(stack);

			try 
			{
				string line;
				while ((line = sr.ReadLine()) != null) 
				{
					if (!FilterLine(line))
						sw.WriteLine(line.Trim());
				}
			} 
			catch (Exception) 
			{
				return stack;
			}
			return sw.ToString();
		}

		static bool FilterLine(string line) 
		{
			var patterns = new[]
			{
				"NUnit.Framework.Assertion", 
				"NUnit.Framework.Assert",
                "System.Reflection.MonoMethod"
			};

		    return patterns.Any(t => line.IndexOf(t,StringComparison.OrdinalIgnoreCase) > 0);
		}

	}
}
