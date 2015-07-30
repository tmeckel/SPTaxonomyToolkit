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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.SharePoint.Client;
using TaxonomyToolkit.Taxml;

namespace TaxonomyToolkitTests
{
    /// <summary>
    ///     This hook captures SharePoint Client OM protocol traffic.  The automated tests
    ///     use this hook to verify that the underlying protocol operations are performed
    ///     as expected (e.g. to detect if additional network roundtrips are performed, which
    ///     would indicate a performance regression).
    /// </summary>
    internal class ClientTestConnectorHook : IDisposable
    {
        private readonly Client15Connector connector;
        private readonly List<string> csomRequests = new List<string>();

        public ClientTestConnectorHook(Client15Connector connector)
        {
            this.connector = connector;
            this.connector.ExecutingQuery += this.connector_ExecutingQuery;
        }

        public void Dispose()
        {
            this.connector.ExecutingQuery -= this.connector_ExecutingQuery;
        }

        public ReadOnlyCollection<string> CsomRequests
        {
            get { return new ReadOnlyCollection<string>(this.csomRequests); }
        }

        public void ClearCsomRequests()
        {
            this.csomRequests.Clear();
        }

        public string GetCsomRequestsAsXml()
        {
            XElement requestsElement = new XElement("Requests");
            XDocument document = new XDocument(requestsElement);

            int index = 0;
            foreach (string request in this.csomRequests)
            {
                var element = new XElement("Request", new XAttribute("Number", index++));

                var parsedRequest = XDocument.Parse(request);
                element.Add(parsedRequest.Nodes());

                requestsElement.Add(element);
            }
            return document.ToString();
        }

        private void connector_ExecutingQuery(object sender, ExecutingQueryEventArgs e)
        {
            var pendingRequest = e.ClientContext.PendingRequest;
            var info_ClientRequest_BuildQuery = typeof (ClientRequest)
                .GetMethod("BuildQuery", BindingFlags.NonPublic | BindingFlags.Instance);

            object chunkStringBuilder = info_ClientRequest_BuildQuery.Invoke(pendingRequest, new object[0]);

            string xml;
            using (var writer = new StringWriter())
            {
                var info_ChunkStringBuilder_WriteContentTo = chunkStringBuilder.GetType()
                    .GetMethod("WriteContentTo", BindingFlags.Public | BindingFlags.Instance);

                info_ChunkStringBuilder_WriteContentTo.Invoke(chunkStringBuilder, new object[1] {writer});
                writer.Close();
                xml = writer.ToString();
            }
            this.csomRequests.Add(xml);
        }

        /// <summary>
        ///     This resets the internal counter used by ClientRequest.NextSequenceId, which prevents
        ///     the protocol dump from being influenced by other CSOM activity that may have occurred
        ///     beforehand.
        ///     WARNING: Since the counter is global, changing it will invalidate any ClientContexts
        ///     that were already constructed before ResetClientRequestSequenceId() was called,
        ///     so don't use this in production code.
        /// </summary>
        public static void ResetClientRequestSequenceId()
        {
            FieldInfo info_sequenceId = typeof (ClientRequest).GetField("s_sequenceId",
                BindingFlags.Static | BindingFlags.NonPublic);
            info_sequenceId.SetValue(null, (long) 1000);
        }
    }
}
