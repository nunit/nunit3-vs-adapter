// ***********************************************************************
// Copyright (c) 2010-2021 Charlie Poole, Terje Sandstrom
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

using System.Globalization;
using System.Xml;

namespace NUnit.VisualStudio.TestAdapter;

/// <summary>
/// XmlHelper provides static methods for basic XML operations.
/// </summary>
public static class XmlHelper
{
    public static XmlNode CreateXmlNode(string xml)
    {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.FirstChild;
        }

    #region Safe Attribute Access

    /// <summary>
    /// Gets the value of the given attribute.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="name">The name.</param>
    public static string GetAttribute(this XmlNode result, string name)
    {
            var attr = result.Attributes?[name];

            return attr?.Value;
        }

    /// <summary>
    /// Gets the value of the given attribute as a double.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <param name="name">The name.</param>
    /// <param name="defaultValue">The default value.</param>
    public static double GetAttribute(this XmlNode result, string name, double defaultValue)
    {
            var attr = result.Attributes[name];

            return attr == null
                ? defaultValue
                : double.Parse(attr.Value, CultureInfo.InvariantCulture);
        }

    #endregion
}