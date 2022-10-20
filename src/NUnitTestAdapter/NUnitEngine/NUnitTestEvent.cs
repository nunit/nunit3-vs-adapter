// ***********************************************************************
// Copyright (c) 2020-2021 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public interface INUnitTestEventForXml
    {
        XmlNode Node { get; }
    }


    public interface INUnitTestEvent : INUnitTestNode
    {
        string Output { get; }
        TimeSpan Duration { get; }
        IEnumerable<NUnitAttachment> NUnitAttachments { get; }
        NUnitTestEvent.CheckedTime StartTime();
        NUnitTestEvent.CheckedTime EndTime();

        bool IsIgnored { get; }
        NUnitTestEvent.ResultType Result();
        bool IsFailed { get; }
        NUnitTestEvent.SiteType Site();
    }

    public abstract class NUnitTestEvent : NUnitTestNode, INUnitTestEvent
    {
        public enum ResultType
        {
            Failed,
            Success,
            Skipped,
            Warning,
            NoIdea
        }

        public enum SiteType
        {
            NoIdea,
            Test,
            Setup,
            TearDown
        }
        public enum TestTypes
        {
            NoIdea,
            TestFixture,
            TestMethod
        }

        public TestTypes TestType()
        {
            string type = Node.GetAttribute("type");
            return type switch
            {
                "TestFixture" => TestTypes.TestFixture,
                "TestMethod" => TestTypes.TestMethod,
                _ => TestTypes.NoIdea
            };
        }

        public ResultType Result()
        {
            string res = Node.GetAttribute("result");
            return res switch
            {
                "Failed" => ResultType.Failed,
                "Passed" => ResultType.Success,
                "Skipped" => ResultType.Skipped,
                "Warning" => ResultType.Warning,
                _ => ResultType.NoIdea
            };
        }

        public bool IsFailed => Result() == ResultType.Failed;

        public SiteType Site()
        {
            string site = Node.GetAttribute("site");
            return site switch
            {
                "SetUp" => SiteType.Setup,
                "TearDown" => SiteType.TearDown,
                _ => SiteType.NoIdea
            };
        }

        public string Label => Node.GetAttribute("label");

        public bool IsIgnored => Label == "Ignored";

        public TimeSpan Duration => TimeSpan.FromSeconds(Node.GetAttribute("duration", 0.0));

        protected NUnitTestEvent(string testEvent) : this(XmlHelper.CreateXmlNode(testEvent))
        { }

        protected NUnitTestEvent(XmlNode node) : base(node)
        {
        }

        public string MethodName => Node.GetAttribute("methodname");
        public string ClassName => Node.GetAttribute("classname");
        public string Output => Node.SelectSingleNode("output")?.InnerText.UnEscapeUnicodeCharacters();


        public CheckedTime StartTime()
        {
            string startTime = Node.GetAttribute("start-time");
            return startTime != null
                ? new CheckedTime { Ok = true, Time = DateTimeOffset.Parse(startTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) }
                : new CheckedTime { Ok = false, Time = DateTimeOffset.Now };
        }

        public CheckedTime EndTime()
        {
            string endTime = Node.GetAttribute("end-time");
            return endTime != null
                ? new CheckedTime { Ok = true, Time = DateTimeOffset.Parse(endTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) }
                : new CheckedTime { Ok = false, Time = DateTimeOffset.Now };
        }

        public struct CheckedTime
        {
            public bool Ok;
            public DateTimeOffset Time;
        }

        private List<NUnitAttachment> nUnitAttachments;


        public IEnumerable<NUnitAttachment> NUnitAttachments
        {
            get
            {
                if (nUnitAttachments != null)
                    return nUnitAttachments;
                nUnitAttachments = new List<NUnitAttachment>();
                foreach (XmlNode attachment in Node.SelectNodes("attachments/attachment"))
                {
                    var path = attachment.SelectSingleNode("filePath")?.InnerText ?? string.Empty;
                    var description = attachment.SelectSingleNode("description")?.InnerText.UnEscapeUnicodeCharacters();
                    nUnitAttachments.Add(new NUnitAttachment(path, description));
                }
                return nUnitAttachments;
            }
        }
    }


    public class NUnitAttachment
    {
        public NUnitAttachment(string path, string description)
        {
            FilePath = path;
            Description = description;
        }

        public string FilePath { get; }

        public string Description { get; }
    }

    public class NUnitProperty
    {
        public string Name { get; }
        public string Value { get; }

        public bool IsInternal => Name.StartsWith("_", StringComparison.Ordinal);

        public NUnitProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public NUnitProperty(XElement node)
        {
            Name = node.Attribute("name").Value;
            Value = node.Attribute("value").Value;
        }
    }

    public class NUnitFailure
    {
        public string Message { get; }
        public string Stacktrace { get; }

        public NUnitFailure(string message, string stacktrace)
        {
            Message = message;
            Stacktrace = stacktrace;
        }
    }


    [Serializable]
    public class NUnitEventWrongTypeException : Exception
    {
        // For guidelines regarding the creation of new exception types, see
        //    https://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    https://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp

        public NUnitEventWrongTypeException()
        {
        }

        public NUnitEventWrongTypeException(string message) : base(message)
        {
        }

        public NUnitEventWrongTypeException(string message, Exception inner) : base(message, inner)
        {
        }

        protected NUnitEventWrongTypeException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
