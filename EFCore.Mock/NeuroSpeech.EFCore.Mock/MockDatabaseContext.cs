using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace NeuroSpeech.EFCore.Mock
{
    /// <summary>
    /// Provides mock context for EF database provider.
    /// 
    /// For example, look at MockSqlDatabaseContext
    /// </summary>
    public abstract class MockDatabaseContext : IDisposable
    {

        private static AsyncLocal<string> currentGuid = new AsyncLocal<string>();

        private const string ContextName = "MockDatabaseContext";

        public virtual string Version => throw new NotImplementedException("Please implement Version property");

        protected List<string> TempFiles { get; } = new List<string>();

        public string CreateTempFile()
        {
            string tmp = Path.GetTempFileName();
            TempFiles.Add(tmp);
            return tmp;
        }

        private StringWriter logger = new StringWriter();

        public virtual void WriteLine(string line)
        {
            logger.WriteLine(line);
        }

        internal MockDatabaseContext()
        {
            //CallContext.LogicalSetData(ContextName, guid);
            currentGuid.Value = guid;
            Contexts[guid] = this;

            OnBeforeInitialize();

            Initialize();

            OnInitialized();
        }

        internal abstract void Initialize();

        protected virtual void OnBeforeInitialize()
        {

        }



        protected virtual void OnInitialized()
        {

        }

        public string ConnectionString { get; internal protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether to delete database after all tests are finished
        /// </summary>
        /// <value>
        ///   <c>true</c> if [do not delete]; otherwise, <c>false</c>.
        /// </value>
        public bool DoNotDelete { get; set; }

        public static MockDatabaseContext Current
        {
            get
            {
                string guid = currentGuid.Value;
                if (guid == null)
                    return null;
                if (Contexts.TryGetValue(guid, out MockDatabaseContext val))
                    return val;
                return null;
            }
        }


        private static ConcurrentDictionary<string, MockDatabaseContext> Contexts = new ConcurrentDictionary<string, MockDatabaseContext>();


        public string DBName { get; protected set; }

        private string guid = Guid.NewGuid().ToString();

        public virtual void Dispose()
        {
            DumpLogs();

            //CallContext.FreeNamedDataSlot(ContextName);
            currentGuid.Value = null;
            Contexts.TryRemove(guid, out MockDatabaseContext mv);

            if (!DoNotDelete)
            {
                DeleteDatabase();
            }

            foreach (var temp in TempFiles)
            {
                try
                {
                    if (System.IO.File.Exists(temp))
                        System.IO.File.Delete(temp);
                }
                catch
                {
                    // swallow.... 
                }
            }

        }

        internal abstract void DeleteDatabase();

        protected virtual void DumpLogs()
        {
            System.Diagnostics.Debug.WriteLine(GeneratedLog);
        }

        public string GeneratedLog => logger.GetStringBuilder().ToString();

        public Dictionary<string, object> Bag { get; } = new Dictionary<string, object>();
    }
}
