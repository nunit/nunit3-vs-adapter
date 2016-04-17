// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

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
                }
            }
            catch(Exception ex)
            {
                _recorder.SendMessage(TestMessageLevel.Error,
                    string.Format("Error processing {0} event for {1}", node.Name, node.GetAttribute("fullname")));
                _recorder.SendMessage(TestMessageLevel.Error, ex.ToString());
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
                        TestMessageLevel.Error,
                        string.Format("{0} failed for test fixture {1}", site, resultNode.GetAttribute("fullname")));

                    var messageNode = resultNode.SelectSingleNode("failure/message");
                    if (messageNode != null)
                        _recorder.SendMessage(TestMessageLevel.Error, messageNode.InnerText);

                    var stackNode = resultNode.SelectSingleNode("failure/stack-trace");
                    if (stackNode != null)
                        _recorder.SendMessage(TestMessageLevel.Error, stackNode.InnerText);
                }
            }
        }
    }
}
