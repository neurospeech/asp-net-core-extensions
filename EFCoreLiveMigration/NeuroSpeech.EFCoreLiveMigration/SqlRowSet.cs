using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public class SqlRowSet : IDisposable
    {

        DbDataReader reader = null;
        DbCommand command = null;

        public SqlRowSet(DbCommand command, DbDataReader reader)
        {
            this.reader = reader;
            this.command = command;
        }

        public bool Read()
        {
            return reader.Read();
        }

        public T GetValue<T>(string name)
        {
            int ordinal = reader.GetOrdinal(name);
            if (reader.IsDBNull(ordinal))
            {
                return default(T);
            }
            object val = reader.GetValue(ordinal);
            Type type = Nullable.GetUnderlyingType(typeof(T));
            if (type == null)
            {
                type = typeof(T);
            }
            if (val.GetType() != type)
                val = (T)Convert.ChangeType(val, type);
            return (T)val;
        }

        public void Dispose()
        {

            this.reader.Dispose();
            this.command.Dispose();
        }

        internal object GetRawValue(string name)
        {
            int ordinal = reader.GetOrdinal(name);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }
            return reader.GetValue(ordinal);
        }
    }
}
