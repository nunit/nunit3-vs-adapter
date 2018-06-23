// ***********************************************************************
// Copyright (c) 2011-2018 Charlie Poole, Terje Sandstrom
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
#if !NETCOREAPP1_0
using System.Runtime.Remoting;
#endif
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.Internal;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// NUnitEventListener implements the EventListener interface and
    /// translates each event into a message for the VS test platform.
    /// </summary>
    public class NUnitEventListener :
#if !NETCOREAPP1_0
        MarshalByRefObject, 
#endif
        ITestEventListener, IDisposable // Public for testing
    {
        private readonly ITestExecutionRecorder _recorder;
        private readonly TestConverter _testConverter;

#if !NETCOREAPP1_0
        public override object InitializeLifetimeService()
        {
            // Give the listener an infinite lease lifetime by returning null
            // http://msdn.microsoft.com/en-us/magazine/cc300474.aspx#edupdate
            // This also means RemotingServices.Disconnect() must be called to prevent memory leaks
            // http://nbevans.wordpress.com/2011/04/17/memory-leaks-with-an-infinite-lifetime-instance-of-marshalbyrefobject/
            return null;
        }
#endif

        public NUnitEventListener(ITestExecutionRecorder recorder, TestConverter testConverter, IDumpXml dumpXml)
        {
            this.dumpXml = dumpXml;
            _recorder = recorder;
            _testConverter = testConverter;
        }

        #region ITestEventListener

        public void OnTestEvent(string report)
        {
            var node = XmlHelper.CreateXmlNode(report);
#if !NETCOREAPP1_0
            dumpXml?.AddTestEvent(node.AsString());
#endif
            try
            {
                switch (node.Name)
                {
                    case "start-test":
                        TestStarted(node);
                        break;

                    case "test-case":
                        TestFinished(node);
                        break;

                    case "test-suite":
                        SuiteFinished(node);
                        break;

                    case "test-output":
                        TestOutput(node);
                        break;
                }
            }
            catch(Exception ex)
            {
                _recorder.SendMessage(TestMessageLevel.Warning,$"Error processing {node.Name} event for {node.GetAttribute("fullname")}");
                _recorder.SendMessage(TestMessageLevel.Warning, ex.ToString());
            }
        }

        #endregion

        #region IDisposable
        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
#if !NETCOREAPP1_0
                    RemotingServices.Disconnect(this);
#endif
                }
            }
            disposed = true;
        }

        ~NUnitEventListener()
        {
            Dispose(false);
        }
        #endregion

        public void TestStarted(XmlNode testNode)
        {
            TestCase ourCase = _testConverter.GetCachedTestCase(testNode.GetAttribute("id"));

            // Simply ignore any TestCase not found in the cache
            if (ourCase != null)
                _recorder.RecordStart(ourCase);
        }

        public void TestFinished(XmlNode resultNode)
        {
            var result = _testConverter.GetVSTestResults(resultNode);
            _recorder.RecordEnd(result.TestCaseResult.TestCase,result.TestCaseResult.Outcome);
            foreach (var vsResult in result.TestResults)
            {
               _recorder.RecordResult(vsResult);
            }
        }

        public void SuiteFinished(XmlNode resultNode)
        {
            var result = resultNode.GetAttribute("result");
            var site = resultNode.GetAttribute("site");
            
            if (result == "Failed")
            {
                if (site == "SetUp" || site == "TearDown")
                {
                    _recorder.SendMessage(
                        TestMessageLevel.Warning,
                        $"{site} failed for test fixture {resultNode.GetAttribute("fullname")}");

                    var messageNode = resultNode.SelectSingleNode("failure/message");
                    if (messageNode != null)
                        _recorder.SendMessage(TestMessageLevel.Warning, messageNode.InnerText);

                    var stackNode = resultNode.SelectSingleNode("failure/stack-trace");
                    if (stackNode != null)
                        _recorder.SendMessage(TestMessageLevel.Warning, stackNode.InnerText);
                }
            }
        }

        private static readonly string NL = Environment.NewLine;
        private static readonly int NL_LENGTH = NL.Length;
        private readonly IDumpXml dumpXml;

        public void TestOutput(XmlNode outputNode)
        {
            var text = outputNode.InnerText;

            // Remove final newline since logger will add one
            if (text.EndsWith(NL))
                text = text.Substring(0, text.Length - NL_LENGTH);

            if (text.IsNullOrWhiteSpace())
            {
                return;
            }
           _recorder.SendMessage(TestMessageLevel.Warning, text);
        }
    }
}
