//------------------------------------------------------------------------------
// <copyright file='StackFrameParser.cs' company='Microsoft Corporation'>
//     Copyright (c) Microsoft Corporation. All Rights Reserved.
//     Information Contained Herein is Proprietary and Confidential.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter.Internal
{
	internal class StackFrameParser
	{
		private readonly Regex frameWithFileInfoRegex;
		private readonly Regex frameWithoutFileInfoRegex;

		public static StackFrameParser CreateStackFrameParser()
		{
			string wordAt = null;
			string wordsInLine = null;

			MethodInfo info = typeof(Environment).GetMethod("GetResourceString", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string) }, null);
			if (info != null)
			{
				wordAt = (string)info.Invoke(null, new object[] { "Word_At" });
				wordsInLine = (string)info.Invoke(null, new object[] { "StackTrace_InFileLineNumber" });
			}

			return new StackFrameParser(wordAt ?? "at", wordsInLine ?? "in {0}:line {1}");
		}

		private StackFrameParser(string wordAtFormat, string wordsInLineFormat)
		{
			string methodName = string.Format(CultureInfo.InvariantCulture, @"(\s*({0})) (?<methodName>[^\\]+)", wordAtFormat);
			frameWithoutFileInfoRegex = new Regex("^" + methodName + "$");

			string wordsInLine = string.Format(CultureInfo.InvariantCulture, wordsInLineFormat, "(?<fileName>.+)", @"(?<lineNumber>\d+)");

			string pattern = methodName + " " + wordsInLine;
			frameWithFileInfoRegex = new Regex("^" + pattern + "$");
		}

		public StackFrame GetStackFrame(string line)
		{
			ValidateArg.NotNullOrEmpty(line, "line");

			Match match = frameWithFileInfoRegex.Match(line);
			if (match.Success)
			{
				string methodName = match.Groups["methodName"].Value;
				string fileName = match.Groups["fileName"].Value;

				int lineNumber;
				if (!int.TryParse(match.Groups["lineNumber"].Value, out lineNumber))
				{
					lineNumber = 0;
				}

				return new StackFrame(methodName, fileName, lineNumber);
			}

			match = frameWithoutFileInfoRegex.Match(line);
			if (match.Success)
			{
				string methodName = match.Groups["methodName"].Value;
				return new StackFrame(methodName);
			}

			return null;
		}
	}
}
