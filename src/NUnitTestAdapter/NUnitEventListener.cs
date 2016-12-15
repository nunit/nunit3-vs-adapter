// ***********************************************************************
// Copyright (c) 2011-2015 Charlie Poole
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
using System.Runtime.Remoting;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Engine;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// NUnitEventListener implements the EventListener interface and
    /// translates each event into a message for the VS test platform.
    /// </summary>
    public class NUnitEventListener : MarshalByRefObject, ITestEventListener, IDisposable // Public for testing
    {
        private readonly ITestExecutionRecorder _recorder;
        private readonly TestConverter _testConverter;

        public override object InitializeLifetimeService()
        {
            // Give the listener an infinite lease lifetime by returning null
            // http://msdn.microsoft.com/en-us/magazine/cc300474.aspx#edupdate
            // This also means RemotingServices.Disconnect() must be called to prevent memory leaks
            // http://nbevans.wordpress.com/2011/04/17/memory-leaks-with-an-infinite-lifetime-instance-of-marshalbyrefobject/
            return null;
        }

        public NUnitEventListener(ITestExecutionRecorder recorder, TestConverter testConverter)
        {
            _recorder = recorder;
            _testConverter = testConverter;
        }

        #region ITestEventListener

        public void OnTestEvent(string report)
        {
            var node = XmlHelper.CreateXmlNode(report);

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
                _recorder.SendMessage(TestMessageLevel.Warning,
                    string.Format("Error processing {0} event for {1}", node.Name, node.GetAttribute("fullname")));
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
                    RemotingServices.Disconnect(this);
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
            TestResult ourResult = _testConverter.ConvertTestResult(resultNode);

            _recorder.RecordEnd(ourResult.TestCase, ourResult.Outcome);
            _recorder.RecordResult(ourResult);
        }

        public void SuiteFinished(XmlNode resultNode)
        {
            var result = resultNode.GetAttribute("result");
            var label = resultNode.GetAttribute("label");
            var site = resultNode.GetAttribute("site");

            if (result == "Failed")
            {
                if (site == "SetUp" || site == "TearDown")
                {
                    _recorder.SendMessage(
                        TestMessageLevel.Warning,
                        string.Format("{0} failed for test fixture {1}", site, resultNode.GetAttribute("fullname")));

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

        public void TestOutput(XmlNode outputNode)
        {
            var testName = outputNode.GetAttribute("testname");
            var stream = outputNode.GetAttribute("stream");
            var text = outputNode.InnerText;

            // Remove final newline since logger will add one
            if (text.EndsWith(NL))
                text = text.Substring(0, text.Length - NL_LENGTH);

            // An empty message will cause SendMessage to throw
            if (text.Length == 0) text = " ";

            _recorder.SendMessage(TestMessageLevel.Warning, text);
        }
    }
}
