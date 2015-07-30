#region MIT License

// Taxonomy Toolkit
// Copyright (c) Microsoft Corporation
// All rights reserved. 
// http://taxonomytoolkit.codeplex.com/
// 
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
// associated documentation files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, publish, distribute, 
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or 
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT 
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace TaxonomyToolkit.General
{
    /// <summary>
    /// This exception class is used by TaxmlLoader to annotate error messages with
    /// line number information.  (Although some exception types such as XmlSchemaException
    /// already include this information, it's in a property and not in the actual message
    /// that the user would see.)
    /// </summary>
    [Serializable]
    public class ParseException : InvalidOperationException
    {
        /// <summary>
        /// Creates a new exception.  If xmlNode contains line number information, it will
        /// be appended to the message.
        /// </summary>
        public ParseException(string message, XObject xmlNode, Exception innerException)
            : base(ParseException.FormatMessage(message, xmlNode), innerException)
        {
        }

        public ParseException(string message, XObject xmlNode)
            : this(message, xmlNode, null)
        {
        }

        /// <summary>
        /// Prepends line number information to the message, if available.
        /// </summary>
        private static string FormatMessage(string message, XObject xmlNode)
        {
            IXmlLineInfo lineInfo = (IXmlLineInfo) xmlNode;

            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                return "[line " + lineInfo.LineNumber + ", col " + lineInfo.LinePosition + "] " + message;
            }

            return message;
        }

        /// <summary>
        /// Rethrows the XmlSchemaExcepion, adding line number information to the message.
        /// </summary>
        public static Exception RethrowWithLineInfo(XmlSchemaException exception)
        {
            if (exception.LineNumber != 0 || exception.LinePosition != 0)
            {
                throw new InvalidOperationException("[line " + exception.LineNumber
                    + ", col " + exception.LinePosition + "] " + exception.Message,
                    exception);
            }

            throw exception;
        }
    }
}
