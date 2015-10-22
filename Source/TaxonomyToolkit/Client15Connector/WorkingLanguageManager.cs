using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;

namespace TaxonomyToolkit.Sync
{
    /// <summary>
    /// The TaxonomySession.WorkingLanguage gets reset at the start of each call to ExecuteQuery(),
    /// and the value is difficult to control.  This means that any CSOM operations that rely
    /// on the working language need to explicitly assign it, and it's safest to do that immediately
    /// before each operation, however with a batching algorithm this would lead to multiple
    /// redundant assignments.  WorkingLanguageManager tracks the assigned value and avoids
    /// redundant assignments.
    /// </summary>
    internal class WorkingLanguageManager
    {
        private class UnmanagedScope : IDisposable
        {
            private WorkingLanguageManager manager;
            public UnmanagedScope(WorkingLanguageManager manager)
            {
                this.manager = manager;
            }
            
            void IDisposable.Dispose()
            {
 	        this.manager.NotifyScopeEnded();
                this.manager = null;
            }
        }

        private Dictionary<ClientObject, int> workingLanguagePerTermStore = new Dictionary<ClientObject, int>();

        private bool inUnmanagedScope = false;

#if DEBUG
        private static FieldInfo info_executionScopes = typeof(ClientRequest)
            .GetField("m_executionScopes", BindingFlags.NonPublic|BindingFlags.Instance);
#endif

        public void SetWorkingLanguageForTermStore(TermStore termStore, int lcid)
        {
            if (this.inUnmanagedScope)
            {
                throw new InvalidOperationException("SetWorkingLanguageForTermStore() may not be called"
                    + " while StartUnmanagedScope() is active");
            }

            this.VerifyNoExecutionScopes(termStore.Context);

            // TODO: As a further optimization, if we know the default value for
            // TaxonomySession.WorkingLanguage, we could avoid setting it to that
            // value at the start of a query.  However, this would add an additional 
            // edge case for testing.
            int previousLcid;
            if (this.workingLanguagePerTermStore.TryGetValue(termStore, out previousLcid))
            {
                if (termStore.WorkingLanguage != previousLcid)
                {
                    throw new InvalidOperationException("The WorkingLanguage was changed outside the WorkingLanguageManager");
                }
                if (lcid == previousLcid)
                {
                    return;
                }
            }
            termStore.WorkingLanguage = lcid;
            this.workingLanguagePerTermStore[termStore] = lcid;
        }

        void VerifyNoExecutionScopes(ClientRuntimeContext clientContext)
        {
#if DEBUG
            var executionScopes = (System.Collections.ICollection)
                info_executionScopes.GetValue(clientContext.PendingRequest);
            if (executionScopes != null && executionScopes.Count > 0)
            {
                throw new InvalidOperationException("SetWorkingLanguageForTermStore() cannot be used inside a conditional scope");
            }
#endif
        }

        /// <summary>
        /// This directly assigns WorkingLanguage without any of the optimizations that are
        /// normally performed by SetWorkingLanguageForTermStore().
        /// </summary>
        public void SetUnmanagedWorkingLanguageForTermStore(TermStore termStore, int lcid)
        {
            if (!this.inUnmanagedScope)
            {
                throw new InvalidOperationException("SetUnmanagedWorkingLanguageForTermStore() cannot"
                    + " be used because StartUnmanagedScope() is not active");
            }

            termStore.WorkingLanguage = lcid;
        }

        /// <summary>
        /// This should be called immediately before ClientContext.ExecuteQuery().
        /// </summary>
        internal void NotifyBeforeExecuteQuery()
        {
            this.workingLanguagePerTermStore.Clear();
        }

        /// <summary>
        /// Use this with a "using" block.  It designates a scope where 
        /// SetUnmanagedWorkingLanguageForTermStore() maybe be used instead of
        /// SetWorkingLanguageForTermStore().
        /// </summary>
        public IDisposable StartUnmanagedScope(TermStore termStore)
        {
            if (this.inUnmanagedScope)
            {
                throw new InvalidOperationException("Another scope is already active");
            }

            this.VerifyNoExecutionScopes(termStore.Context);

            this.workingLanguagePerTermStore.Clear();

            this.inUnmanagedScope = true;
            return new UnmanagedScope(this);
        }

        private void NotifyScopeEnded()
        {
            Debug.Assert(this.workingLanguagePerTermStore.Count == 0);
            this.inUnmanagedScope = false;
        }
    }
}
